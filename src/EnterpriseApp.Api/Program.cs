using EnterpriseApp.Api.Middleware;
using EnterpriseApp.Application;
using EnterpriseApp.Application.Interfaces.Security;
using EnterpriseApp.Infrastructure;
using EnterpriseApp.Infrastructure.Persistence;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add API Controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Clean Architecture: Application & Infrastructure Layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// CORS Policy for Blazor Client / Frontend access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Swagger / OpenAPI with JWT Bearer configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Enterprise Solution Clean Architecture API",
        Version = "v1",
        Description = "Enterprise-grade multi-module API featuring Clean Architecture, JWT security, Azure Service Bus messaging, and multi-threading."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Authorization header using Bearer scheme. Enter token.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
    });
});

var app = builder.Build();

// Configure Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Enterprise Solution API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAllOrigins");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Initialize & Seed Database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EnterpriseDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbInitializer.InitializeAsync(context, hasher, logger);

    if (args.Contains("--seed-only"))
    {
        var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(context.Users);
        var books = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.Books);
        var customers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.Customers);
        var products = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.Products);

        Console.WriteLine("\n=======================================================");
        Console.WriteLine(" Database Migration & Seeding Completed Successfully! ");
        Console.WriteLine("=======================================================");
        Console.WriteLine($"[Seeded Users: {users.Count}]");
        foreach (var u in users)
        {
            Console.WriteLine($"  - Username: {u.Username,-10} | Role: {u.Role,-8} | Email: {u.Email}");
        }
        Console.WriteLine($"[Seeded Books: {books}]");
        Console.WriteLine($"[Seeded Customers: {customers}]");
        Console.WriteLine($"[Seeded Products: {products}]");
        Console.WriteLine("=======================================================\n");
        return;
    }
}

app.Run();

public partial class Program { }
