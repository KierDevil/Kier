using DepartmentFinancialRecords.API.Data;
using DepartmentFinancialRecords.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var jwtKey = builder.Configuration["Jwt:Key"];
var allowedCorsOrigins = (builder.Configuration["AllowedCorsOrigins"] ?? "http://localhost:5173,https://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
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

    try
    {
        dbContext.Database.ExecuteSqlRaw("ALTER TABLE Students ADD COLUMN RfidUid longtext NOT NULL");
    }
    catch
    {
        // Existing local databases already have this column after the first RFID startup.
    }

    foreach (var sql in new[]
    {
        "ALTER TABLE Students MODIFY StudentId varchar(64) NOT NULL",
        "ALTER TABLE Students MODIFY RfidUid varchar(128) NOT NULL",
        "ALTER TABLE AttendanceEvents MODIFY Title varchar(200) NOT NULL",
        "CREATE UNIQUE INDEX IX_Students_StudentId ON Students (StudentId)",
        "CREATE INDEX IX_Students_RfidUid ON Students (RfidUid)",
        "CREATE UNIQUE INDEX IX_AttendanceRecords_StudentId_AttendanceEventId ON AttendanceRecords (StudentId, AttendanceEventId)"
    })
    {
        try
        {
            dbContext.Database.ExecuteSqlRaw(sql);
        }
        catch
        {
            // Existing databases may already have these columns/indexes.
        }
    }

    if (!dbContext.Students.Any())
    {
        dbContext.Students.AddRange(
            new Student { StudentId = "2026-001", FirstName = "Mika", LastName = "Reyes", Course = "BSIT", Section = "3A", ContactNumber = "0917 100 1101", RfidUid = "RFID2026001" },
            new Student { StudentId = "2026-014", FirstName = "Aaron", LastName = "Cruz", Course = "BSCS", Section = "2B", ContactNumber = "0918 200 2202", RfidUid = "RFID2026014" },
            new Student { StudentId = "2026-027", FirstName = "Lia", LastName = "Santos", Course = "BSIS", Section = "4A", ContactNumber = "0919 300 3303", RfidUid = "RFID2026027" },
            new Student { StudentId = "2026-035", FirstName = "Noah", LastName = "Dela Cruz", Course = "BSIT", Section = "1C", ContactNumber = "0920 400 4404", RfidUid = "RFID2026035" });
        dbContext.SaveChanges();
    }
    else
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
