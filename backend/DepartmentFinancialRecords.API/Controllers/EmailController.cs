using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text.RegularExpressions;

namespace DepartmentFinancialRecords.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public EmailController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("receipt")]
        public IActionResult SendReceipt([FromBody] ReceiptEmailRequest request)
        {
            var provider = _configuration["Email:Provider"] ?? "gmail";
            var providerSection = _configuration.GetSection($"Email:{provider}");

            var smtpHost = providerSection["SmtpHost"] ?? _configuration["Email:SmtpHost"];
            var smtpPortValue = providerSection["SmtpPort"] ?? _configuration["Email:SmtpPort"];
            var smtpUsername = providerSection["SmtpUsername"] ?? _configuration["Email:SmtpUsername"];
            var smtpPassword = providerSection["SmtpPassword"] ?? _configuration["Email:SmtpPassword"];
            var fromAddress = providerSection["FromAddress"] ?? _configuration["Email:FromAddress"];
            var enableSslValue = providerSection["EnableSsl"] ?? _configuration["Email:EnableSsl"];

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword) || string.IsNullOrWhiteSpace(fromAddress))
            {
                return BadRequest(new { message = "Email SMTP settings are not configured." });
            }

            if (string.IsNullOrWhiteSpace(request.ToEmail) || !MailAddress.TryCreate(request.ToEmail, out _))
            {
                return BadRequest(new { message = "A valid recipient email is required." });
            }

            if (!int.TryParse(smtpPortValue, out var smtpPort))
            {
                return BadRequest(new { message = "Email SMTP port is invalid." });
            }

            var details = string.IsNullOrWhiteSpace(request.Details)
                ? string.Empty
                : $"""

                Paid event fine(s):
                {request.Details.Trim()}
                """;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = $"Receipt {request.ReceiptNumber}",
                Body = $"""
                Hello,

                This is a receipt confirmation for your payment.

                Receipt: {request.ReceiptNumber}
                Student: {request.StudentName}
                Category: {request.Category}
                Amount: {request.Amount}
                Date: {request.Date}
                {details}

                Thank you,
                Kier Records
                """,
                IsBodyHtml = false
            };

            mailMessage.To.Add(new MailAddress(request.ToEmail));

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = bool.TryParse(enableSslValue, out var enableSsl) && enableSsl
            };

            try
            {
                client.Send(mailMessage);
                return Ok(new { message = "Receipt email sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to send receipt email.", error = ex.Message });
            }
        }

        [HttpPost("student-qr")]
        public IActionResult SendStudentQr([FromBody] StudentQrEmailRequest request)
        {
            var provider = _configuration["Email:Provider"] ?? "gmail";
            var providerSection = _configuration.GetSection($"Email:{provider}");

            var smtpHost = providerSection["SmtpHost"] ?? _configuration["Email:SmtpHost"];
            var smtpPortValue = providerSection["SmtpPort"] ?? _configuration["Email:SmtpPort"];
            var smtpUsername = providerSection["SmtpUsername"] ?? _configuration["Email:SmtpUsername"];
            var smtpPassword = providerSection["SmtpPassword"] ?? _configuration["Email:SmtpPassword"];
            var fromAddress = providerSection["FromAddress"] ?? _configuration["Email:FromAddress"];
            var enableSslValue = providerSection["EnableSsl"] ?? _configuration["Email:EnableSsl"];

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword) || string.IsNullOrWhiteSpace(fromAddress))
            {
                return BadRequest(new { message = "Email SMTP settings are not configured." });
            }

            if (string.IsNullOrWhiteSpace(request.ToEmail) || !MailAddress.TryCreate(request.ToEmail, out _))
            {
                return BadRequest(new { message = "A valid recipient email is required." });
            }

            if (!int.TryParse(smtpPortValue, out var smtpPort))
            {
                return BadRequest(new { message = "Email SMTP port is invalid." });
            }

            if (string.IsNullOrWhiteSpace(request.QrImageBase64))
            {
                return BadRequest(new { message = "A QR code image is required." });
            }

            var match = Regex.Match(request.QrImageBase64, "^data:image/(?:png|jpeg|jpg|gif|webp);base64,(.*)$", RegexOptions.IgnoreCase);
            var base64 = match.Success ? match.Groups[1].Value : request.QrImageBase64;

            byte[] imageBytes;
            try
            {
                imageBytes = Convert.FromBase64String(base64.Trim());
            }
            catch
            {
                return BadRequest(new { message = "The QR code data is not valid Base64 content." });
            }

            var html = $"""
            <html>
              <body style="font-family: Arial, sans-serif; color:#1f2a37; padding:24px;">
                <h2>Welcome, {System.Net.WebUtility.HtmlEncode(request.StudentName)}</h2>
                <p>Your student QR code is ready.</p>
                <p><strong>Student ID:</strong> {System.Net.WebUtility.HtmlEncode(request.StudentNo)}<br />
                <strong>Course:</strong> {System.Net.WebUtility.HtmlEncode(request.Course ?? "")}</p>
                <div style="margin: 24px 0;">
                  <img src="cid:studentQr" alt="Student QR code" style="max-width:240px; border: 1px solid #d0d7de; padding: 12px; background:#fff;" />
                </div>
                <p>Please keep this QR code for attendance scanning.</p>
              </body>
            </html>
            """;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = $"Your student QR code - {request.StudentNo}",
                IsBodyHtml = true,
                Body = html,
            };
            mailMessage.To.Add(new MailAddress(request.ToEmail));

            var alternateView = AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html);
            var linkedResource = new LinkedResource(new MemoryStream(imageBytes), new ContentType("image/png"))
            {
                ContentId = "studentQr",
                TransferEncoding = TransferEncoding.Base64,
            };
            alternateView.LinkedResources.Add(linkedResource);
            mailMessage.AlternateViews.Add(alternateView);

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = bool.TryParse(enableSslValue, out var enableSsl) && enableSsl
            };

            try
            {
                client.Send(mailMessage);
                return Ok(new { message = "Student QR email sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to send student QR email.", error = ex.Message });
            }
        }

        [HttpPost("fine-notice")]
        public IActionResult SendFineNotice([FromBody] FineNoticeEmailRequest request)
        {
            var provider = _configuration["Email:Provider"] ?? "gmail";
            var providerSection = _configuration.GetSection($"Email:{provider}");

            var smtpHost = providerSection["SmtpHost"] ?? _configuration["Email:SmtpHost"];
            var smtpPortValue = providerSection["SmtpPort"] ?? _configuration["Email:SmtpPort"];
            var smtpUsername = providerSection["SmtpUsername"] ?? _configuration["Email:SmtpUsername"];
            var smtpPassword = providerSection["SmtpPassword"] ?? _configuration["Email:SmtpPassword"];
            var fromAddress = providerSection["FromAddress"] ?? _configuration["Email:FromAddress"];
            var enableSslValue = providerSection["EnableSsl"] ?? _configuration["Email:EnableSsl"];

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword) || string.IsNullOrWhiteSpace(fromAddress))
            {
                return BadRequest(new { message = "Email SMTP settings are not configured." });
            }

            if (string.IsNullOrWhiteSpace(request.ToEmail) || !MailAddress.TryCreate(request.ToEmail, out _))
            {
                return BadRequest(new { message = "A valid recipient email is required." });
            }

            if (!int.TryParse(smtpPortValue, out var smtpPort))
            {
                return BadRequest(new { message = "Email SMTP port is invalid." });
            }

            var customMessage = string.IsNullOrWhiteSpace(request.CustomMessage)
                ? string.Empty
                : $"""

                Message:
                {request.CustomMessage.Trim()}
                """;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = $"Attendance fine notice - {request.EventTitle}",
                Body = $"""
                Hello {request.StudentName},

                You were marked absent for this attendance event.

                Event: {request.EventTitle}
                Session: {request.SessionType}
                New fine: {request.NewFineAmount}
                Total unpaid fines: {request.TotalUnpaidFines}
                Date: {request.Date}
                {customMessage}

                If you already settled this, please coordinate with the officer/admin.

                Thank you,
                Kier Records
                """,
                IsBodyHtml = false
            };

            mailMessage.To.Add(new MailAddress(request.ToEmail));

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = bool.TryParse(enableSslValue, out var enableSsl) && enableSsl
            };

            try
            {
                client.Send(mailMessage);
                return Ok(new { message = "Fine notice email sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to send fine notice email.",
                    error = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("message")]
        public IActionResult SendMessage([FromBody] GeneralEmailRequest request)
        {
            var provider = _configuration["Email:Provider"] ?? "gmail";
            var providerSection = _configuration.GetSection($"Email:{provider}");

            var smtpHost = providerSection["SmtpHost"] ?? _configuration["Email:SmtpHost"];
            var smtpPortValue = providerSection["SmtpPort"] ?? _configuration["Email:SmtpPort"];
            var smtpUsername = providerSection["SmtpUsername"] ?? _configuration["Email:SmtpUsername"];
            var smtpPassword = providerSection["SmtpPassword"] ?? _configuration["Email:SmtpPassword"];
            var fromAddress = providerSection["FromAddress"] ?? _configuration["Email:FromAddress"];
            var enableSslValue = providerSection["EnableSsl"] ?? _configuration["Email:EnableSsl"];

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword) || string.IsNullOrWhiteSpace(fromAddress))
            {
                return BadRequest(new { message = "Email SMTP settings are not configured." });
            }

            if (string.IsNullOrWhiteSpace(request.ToEmail) || !MailAddress.TryCreate(request.ToEmail, out _))
            {
                return BadRequest(new { message = "A valid recipient email is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Message is required." });
            }

            if (!int.TryParse(smtpPortValue, out var smtpPort))
            {
                return BadRequest(new { message = "Email SMTP port is invalid." });
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = string.IsNullOrWhiteSpace(request.Subject) ? "Kier Records notice" : request.Subject.Trim(),
                Body = $"""
                Hello {request.StudentName},

                {request.Message.Trim()}

                Date: {request.Date}

                Thank you,
                Kier Records
                """,
                IsBodyHtml = false
            };

            mailMessage.To.Add(new MailAddress(request.ToEmail));

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = bool.TryParse(enableSslValue, out var enableSsl) && enableSsl
            };

            try
            {
                client.Send(mailMessage);
                return Ok(new { message = "Email message sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to send email message.",
                    error = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }
    }

    public record ReceiptEmailRequest(
        string ToEmail,
        string ReceiptNumber,
        string StudentName,
        string Category,
        string? Details,
        string Amount,
        string Date);

    public record StudentQrEmailRequest(
        string ToEmail,
        string StudentName,
        string StudentNo,
        string? Course,
        string QrImageBase64);

    public record FineNoticeEmailRequest(
        string ToEmail,
        string StudentName,
        string EventTitle,
        string SessionType,
        string NewFineAmount,
        string TotalUnpaidFines,
        string? CustomMessage,
        string Date);

    public record GeneralEmailRequest(
        string ToEmail,
        string StudentName,
        string Subject,
        string Message,
        string Date);
}
