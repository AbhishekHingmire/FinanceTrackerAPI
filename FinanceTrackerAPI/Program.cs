using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.Middleware;
using FinanceTrackerAPI.Services.Implementations;
using FinanceTrackerAPI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Build SQL Server Connection String with Multi-Source Priority:
// 1) MonsterASP / RunASP env var override (DefaultConnection_ConnectionStrings or ConnectionStrings__DefaultConnection)
// 2) Standard ASP.NET Core Configuration.GetConnectionString("DefaultConnection")
// 3) Local development SQL Server instance
var envConnString = Environment.GetEnvironmentVariable("DefaultConnection_ConnectionStrings")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings_DefaultConnection")
    ?? Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

var baseConnectionString = !string.IsNullOrWhiteSpace(envConnString)
    ? envConnString
    : "Data Source=127.0.0.1,1433;Initial Catalog=FinanceTrackerDb;Persist Security Info=True;User ID=sa;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=30;Encrypt=True;TrustServerCertificate=True;Command Timeout=0";

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
builder.Services.AddHttpClient();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ISmsService, TextplateSmsService>();
builder.Services.AddScoped<IErrorLogService, ErrorLogService>();
builder.Services.AddScoped<IMarketDataService, MarketDataService>();
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
                "http://localhost:4173",
                "http://localhost:8080",
                "https://onefinancetracker.vercel.apps")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WealthTrack Finance API",
        Version = "v1",
        Description = "Enterprise .NET 10 Web API for WealthTrack Personal Finance with JWT Bearer Authentication and SQL Server 2022 backend.",
        Contact = new OpenApiContact
        {
            Name = "WealthTrack Engineering Team"
        }
    });

    // Support Bearer Token in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT Bearer token (without 'Bearer ' prefix). Example: eyJhbGciOiJIUzI1NiIsIn..."
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc),
            new List<string>()
        }
    });

    // Include XML Documentation Comments if file exists
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddOpenApi();

var app = builder.Build();

// 5. Middleware Pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

// Enable Swagger in Development and Local environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WealthTrack API v1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
    c.EnableTryItOutByDefault();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("WealthTrackCors");

app.UseAuthentication();
app.UseAuthorization();

// Redirect root / to /swagger for instant access
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapControllers();

// 6. Automatic Database Migration on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = services.GetRequiredService<FinanceTrackerDbContext>();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending database migration(s)...", pendingMigrations.Count());
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("Database is up to date. No pending migrations.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while automatically applying database migrations on startup. Please check SQL Server connectivity and user-secrets password.");
    }
}

app.Run();
