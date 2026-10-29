using System.Text;
using EnterpriseApp.Application.Interfaces.Repositories;
using EnterpriseApp.Application.Interfaces.Security;
using EnterpriseApp.Application.Interfaces.Services;
using EnterpriseApp.Infrastructure.Messaging;
using EnterpriseApp.Infrastructure.Persistence;
using EnterpriseApp.Infrastructure.Persistence.Repositories;
using EnterpriseApp.Infrastructure.Security;
using EnterpriseApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Database (SQLite)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=EnterpriseApp.db";

        services.AddDbContext<EnterpriseDbContext>(options =>
            options.UseSqlite(connectionString));

        // 2. Concrete Repositories
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // 3. Security & JWT
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        // 4. JWT Authentication
        var secretKey = configuration["Jwt:SecretKey"]
            ?? "EnterpriseSuperSecretSecureKeyForJwtTokens2026_WithMinimumLengthRequirement!";
        var issuer = configuration["Jwt:Issuer"] ?? "EnterpriseAppApi";
        var audience = configuration["Jwt:Audience"] ?? "EnterpriseAppClients";

        services.AddAuthentication(options =>
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
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
            options.AddPolicy("RequireManagerOrAdmin", policy => policy.RequireRole("Admin", "Manager"));
        });

        // 5. Factorial Concurrent Calculator
        services.AddSingleton<IFactorialCalculatorService, FactorialCalculatorService>();

        // 6. Azure Service Bus Producer-Consumer Pipeline
        services.AddSingleton<LocalServiceBusQueue>();
        services.AddSingleton<IMessageConsumerLogService, MessageConsumerLogService>();
        services.AddSingleton<IMessageProducerService, AzureServiceBusProducerService>();
        services.AddHostedService<AzureServiceBusConsumerHostedService>();

        // 7. Weather API Consumer with HttpClientFactory
        services.AddHttpClient<IWeatherService, WeatherService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("User-Agent", "EnterpriseWeatherClient/1.0");
        });

        return services;
    }
}
