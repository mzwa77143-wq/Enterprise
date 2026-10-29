using EnterpriseApp.Application.Interfaces.Security;
using EnterpriseApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseApp.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        EnterpriseDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger)
    {
        try
        {
            await context.Database.MigrateAsync();

            if (!await context.Users.AnyAsync())
            {
                logger.LogInformation("Seeding initial users...");

                var admin = new User(
                    "admin",
                    "admin@enterprise.local",
                    passwordHasher.HashPassword("Admin@123456"),
                    "Admin");

                var manager = new User(
                    "manager",
                    "manager@enterprise.local",
                    passwordHasher.HashPassword("Manager@123456"),
                    "Manager");

                var user = new User(
                    "user",
                    "user@enterprise.local",
                    passwordHasher.HashPassword("User@123456"),
                    "User");

                await context.Users.AddRangeAsync(admin, manager, user);
            }

            if (!await context.Books.AnyAsync())
            {
                logger.LogInformation("Seeding initial library books...");

                var books = new List<Book>
                {
                    new("Clean Architecture: A Craftsman's Guide to Software Structure", "Robert C. Martin", "978-0134494166", 2017),
                    new("Domain-Driven Design: Tackling Complexity in the Heart of Software", "Eric Evans", "978-0321125217", 2003),
                    new("Building Microservices: Designing Fine-Grained Systems", "Sam Newman", "978-1492034025", 2021),
                    new("Concurrency in C# Cookbook", "Stephen Cleary", "978-1492054504", 2019),
                    new("Enterprise Integration Patterns", "Gregor Hohpe", "978-0321200686", 2003)
                };

                await context.Books.AddRangeAsync(books);
            }

            if (!await context.Customers.AnyAsync())
            {
                logger.LogInformation("Seeding initial customers...");

                var customers = new List<Customer>
                {
                    new("Acme Corporation", "contact@acme.com", "+1-555-0101"),
                    new("Wayne Enterprises", "procurement@waynecorp.com", "+1-555-0102"),
                    new("Stark Industries", "tony@starkindustries.com", "+1-555-0103"),
                    new("Cyberdyne Systems", "info@cyberdyne.net", "+1-555-0104")
                };

                await context.Customers.AddRangeAsync(customers);
            }

            if (!await context.Products.AnyAsync())
            {
                logger.LogInformation("Seeding initial products catalog...");

                var products = new List<Product>
                {
                    new("Enterprise Cloud Suite", "Comprehensive cloud orchestration and monitoring platform", 1499.99m, "Software"),
                    new("Quantum Computing SDK", "High-performance developer toolkit for simulated quantum algorithms", 899.50m, "Developer Tools"),
                    new("Cyber Security Sentinel", "Real-time threat intelligence and zero-trust authentication gateway", 2499.00m, "Security"),
                    new("Edge AI Vision System", "Low-latency neural network inference pipeline for IoT sensors", 1250.00m, "Hardware & AI")
                };

                await context.Products.AddRangeAsync(products);
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Database initialization and seed complete.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing and seeding the database.");
            throw;
        }
    }
}
