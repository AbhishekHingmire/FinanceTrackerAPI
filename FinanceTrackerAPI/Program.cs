using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.Middleware;
using FinanceTrackerAPI.Services.Implementations;
using FinanceTrackerAPI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Build SQL Server Connection String with Secure Password from User Secrets / Environment Variables
var baseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=127.0.0.1,1433;Initial Catalog=FinanceTrackerDb;Persist Security Info=True;User ID=sa;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=30;Encrypt=True;TrustServerCertificate=True;Command Timeout=0";

// Check if password is provided in User Secrets, Environment, or Configuration
var dbPassword = builder.Configuration["Database:Password"]
    ?? Environment.GetEnvironmentVariable("DB_PASSWORD")
    ?? Environment.GetEnvironmentVariable("SA_PASSWORD")
    ?? Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD");

var connBuilder = new SqlConnectionStringBuilder(baseConnectionString);
if (!string.IsNullOrWhiteSpace(dbPassword))
{
    connBuilder.Password = dbPassword;
}

builder.Services.AddDbContext<FinanceTrackerDbContext>(options =>
{
    options.UseSqlServer(connBuilder.ConnectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
        sqlOptions.MigrationsAssembly(typeof(FinanceTrackerDbContext).Assembly.FullName);
    });
});

// 2. Application Services Dependency Injection
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 3. Configure JWT Authentication & Authorization
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? "WealthTrackSuperSecretKey_ProductionGrade_256BitsMinLength_2026";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WealthTrackAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WealthTrackClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in prod with public certs
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    // Custom JSON responses on 401 Unauthorized
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var response = ApiResponse<object>.FailureResult("Authentication token is missing, expired, or invalid.");
            await context.Response.WriteAsJsonAsync(response);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var response = ApiResponse<object>.FailureResult("You do not have permission to access this resource.");
            await context.Response.WriteAsJsonAsync(response);
        }
    };
});

builder.Services.AddAuthorization();

// 4. CORS Policy for React Vite Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("WealthTrackCors", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:3000",
                "http://localhost:4173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// 5. Middleware Pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("WealthTrackCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
