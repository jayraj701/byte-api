using Byte.Api.Configuration;
using Byte.Api.Services;
using Byte.Domain.Interfaces;
using Byte.Infra.Data;
using Byte.Infra.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Byte.Api;

public static class IOC
{
    public static IServiceCollection AddDependencies(this IServiceCollection services, IConfiguration config)
    {
        services
            .AddPersistence(config)
            .AddJwtAuthentication(config)
            .AddAuthorisationPolicies()
            .AddHealthMonitoring();

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                    sqlOptions.CommandTimeout(30);
                }));

        // Repositories
        services.AddScoped<IPayrollBatchRepository, PayrollBatchRepository>();
        services.AddScoped<IPayrollRecordRepository, PayrollRecordRepository>();
        services.AddScoped<IPayrollCalculationRepository, PayrollCalculationRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // Application services
        services.AddScoped<FileParserService>();
        services.AddScoped<AttendanceService>();
        services.AddScoped<PayrollCalculationService>();
        services.AddScoped<ApprovalService>();

        // Options
        services.Configure<PayrollRulesOptions>(config.GetSection("PayrollRules"));

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        return services;
    }

    private static IServiceCollection AddAuthorisationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
        });

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000", "http://localhost:5173"];

        services.AddCors(options =>
        {
            options.AddPolicy("FrontendPolicy", policy =>
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        return services;
    }

    private static IServiceCollection AddHealthMonitoring(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>();

        return services;
    }
}
