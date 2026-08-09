using DepartmentFinancialRecords.API.Data;
using DepartmentFinancialRecords.API.Models;
using DepartmentFinancialRecords.API.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DepartmentFinancialRecords.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            var username = (request.Username ?? string.Empty).Trim();
            var password = request.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return BadRequest(new { message = "Username and password are required." });
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(item => item.Username == username && item.IsActive);

            if (user is null || !PasswordHasher.VerifyPassword(password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }

            var token = GenerateJwtToken(user);
            return Ok(new LoginResponse(
                user.Id,
                user.Username,
                user.Role.ToString(),
                token));
        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
        {
            var username = (request.Username ?? string.Empty).Trim();
            var password = request.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return BadRequest(new { message = "Username and password are required." });
            }

            if (password.Length < 6)
            {
                return BadRequest(new { message = "Password must be at least 6 characters long." });
            }

            var exists = await _dbContext.Users.AnyAsync(item => item.Username == username && item.IsActive);
            if (exists)
            {
                return Conflict(new { message = "Username is already taken." });
            }

            var user = new User
            {
                Username = username,
                PasswordHash = PasswordHasher.HashPassword(password),
                Role = Enum.TryParse<UserRole>(request.Role, true, out var role) ? role : UserRole.Officer,
                IsActive = true
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return Ok(new LoginResponse(user.Id, user.Username, user.Role.ToString(), token));
        }

        [Authorize]
        [HttpGet("me")]
        public ActionResult<object> Me()
        {
            var claims = User.Claims;
            var username = claims.FirstOrDefault(item => item.Type == ClaimTypes.Name)?.Value ?? string.Empty;
            var role = claims.FirstOrDefault(item => item.Type == ClaimTypes.Role)?.Value ?? string.Empty;

            return Ok(new { username, role });
        }

        private string GenerateJwtToken(User user)
        {
            var secretKey = _configuration["Jwt:Key"]
                ?? Environment.GetEnvironmentVariable("JWT_KEY")
                ?? "ChangeThisKeyBeforeProductionUse";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("userId", user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public record LoginRequest(string Username, string Password);
    public record RegisterRequest(string Username, string Password, string? Role);
    public record LoginResponse(int UserId, string Username, string Role, string Token);
}
