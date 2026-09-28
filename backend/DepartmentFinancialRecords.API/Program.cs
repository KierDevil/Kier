using DepartmentFinancialRecords.API.Data;
using DepartmentFinancialRecords.API.Models;
using DepartmentFinancialRecords.API.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_KEY");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection using User Secrets or an environment variable.");
}

if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("Configure Jwt:Key with a secret of at least 32 characters using User Secrets or an environment variable.");
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
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
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        dbContext.Database.EnsureCreated();

        if (!dbContext.Users.Any())
        {
            var adminUsername = builder.Configuration["BootstrapAdmin:Username"]
                ?? Environment.GetEnvironmentVariable("APP_ADMIN_USERNAME");
            var adminPassword = builder.Configuration["BootstrapAdmin:Password"]
                ?? Environment.GetEnvironmentVariable("APP_ADMIN_PASSWORD");
            if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException("Configure BootstrapAdmin:Username and BootstrapAdmin:Password before the first database startup.");
            }

            var usersToSeed = new List<User>
            {
                new()
                {
                    Username = adminUsername.Trim(),
                    PasswordHash = PasswordHasher.HashPassword(adminPassword),
                    Role = UserRole.Administrator,
                    IsActive = true
                }
            };

            var userUsername = builder.Configuration["UserSeed:Username"]
                ?? Environment.GetEnvironmentVariable("APP_USER_USERNAME");
            var userPassword = builder.Configuration["UserSeed:Password"]
                ?? Environment.GetEnvironmentVariable("APP_USER_PASSWORD");
            if (string.IsNullOrWhiteSpace(userUsername) != string.IsNullOrWhiteSpace(userPassword))
            {
                throw new InvalidOperationException("Configure both UserSeed:Username and UserSeed:Password, or leave both unset.");
            }

            if (!string.IsNullOrWhiteSpace(userUsername))
            {
                usersToSeed.Add(new User
                {
                    Username = userUsername.Trim(),
                    PasswordHash = PasswordHasher.HashPassword(userPassword!),
                    Role = UserRole.Student,
                    IsActive = true
                });
            }

            dbContext.Users.AddRange(usersToSeed);
            dbContext.SaveChanges();
        }
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Database initialization failed.");
        throw;
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
