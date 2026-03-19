using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NcaaBracket.Api.Data;
using NcaaBracket.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// EF Core with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "https://bracketology.ca", "https://www.bracketology.ca")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Register services
builder.Services.AddSingleton<GoogleAuthService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<ScoringService>();
builder.Services.AddHostedService<EspnSyncService>();

builder.Services.AddControllers();

var app = builder.Build();

// Auto-migrate and seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // If tables exist but no migration history, mark initial migration as applied
    var pendingMigrations = db.Database.GetPendingMigrations().ToList();
    var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
    if (pendingMigrations.Contains("20260319133119_InitialCreate") && appliedMigrations.Count == 0)
    {
        try
        {
            // Check if tables already exist (created by init.sql)
            var tableExists = db.Database.ExecuteSqlRaw(
                "SELECT 1 FROM information_schema.tables WHERE table_name = 'users'") > 0;
        }
        catch
        {
            // Tables don't exist, let migration create them
        }

        // If we get here without exception, tables exist — mark migration as applied
        try
        {
            db.Database.ExecuteSqlRaw(
                @"CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                    ""MigrationId"" varchar(150) NOT NULL,
                    ""ProductVersion"" varchar(32) NOT NULL,
                    CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
                )");
            db.Database.ExecuteSqlRaw(
                @"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                  VALUES ('20260319133119_InitialCreate', '9.0.3')
                  ON CONFLICT DO NOTHING");
        }
        catch { /* ignore if already exists */ }
    }

    db.Database.Migrate();

    // Seed default tournament settings if none exist
    if (!db.TournamentSettings.Any())
    {
        db.TournamentSettings.Add(new NcaaBracket.Api.Models.TournamentSettings
        {
            Year = 2026,
            LockDate = new DateTime(2026, 3, 20, 4, 59, 0, DateTimeKind.Utc),
            IsLocked = false
        });
        db.SaveChanges();
    }
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
