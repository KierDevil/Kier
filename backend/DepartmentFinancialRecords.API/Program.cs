using DepartmentFinancialRecords.API.Data;
using DepartmentFinancialRecords.API.Models;
using DepartmentFinancialRecords.API.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? "server=127.0.0.1;port=3308;database=departmentfinancialrecords;user=kier;SslMode=None;AllowPublicKeyRetrieval=True;";
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_KEY")
    ?? "KierDepartmentRecordsJwtSecretKey2026!";

if (string.IsNullOrWhiteSpace(jwtKey))
{
    jwtKey = "KierDepartmentRecordsJwtSecretKey2026!";
}

if (jwtKey.Length < 32)
{
    jwtKey = jwtKey.PadRight(32, '!');
}

var allowedCorsOrigins = (builder.Configuration["AllowedCorsOrigins"] ?? Environment.GetEnvironmentVariable("ALLOWED_CORS_ORIGINS") ?? "*")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .ToArray();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedCorsOrigins.Any(origin => origin.Trim('"') == "*"))
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(allowedCorsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Missing DefaultConnection in configuration.");
    }

    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey ?? "ReplaceWithSecureKeyForLocalDevelopmentOnly"))
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();

    // Do not seed demo student data. Keep the database empty until real student records
    // are created through the application or imported externally.
    if (dbContext.Students.Any())
    {
        var demoRfids = new Dictionary<string, string>
        {
            ["2026-001"] = "RFID2026001",
            ["2026-014"] = "RFID2026014",
            ["2026-027"] = "RFID2026027",
            ["2026-035"] = "RFID2026035"
        };

        foreach (var student in dbContext.Students.Where(student => string.IsNullOrWhiteSpace(student.RfidUid)))
        {
            if (demoRfids.TryGetValue(student.StudentId, out var rfidUid))
            {
                student.RfidUid = rfidUid;
            }
        }

        dbContext.SaveChanges();
    }

    if (!dbContext.Users.Any())
    {
        var adminUsername = Environment.GetEnvironmentVariable("APP_ADMIN_USERNAME") ?? "admin";
        var adminPassword = Environment.GetEnvironmentVariable("APP_ADMIN_PASSWORD") ?? "Admin123!";

        dbContext.Users.Add(new User
        {
            Username = adminUsername,
            PasswordHash = PasswordHasher.HashPassword(adminPassword),
            Role = UserRole.Administrator,
            IsActive = true
        });

        dbContext.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
