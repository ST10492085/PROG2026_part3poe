using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PROG2026_part2.Data;
using PROG2026_part2.Services;

namespace PROG2026_part2.Tests;

/// <summary>
/// Starts the real RaceDay API inside the test process, but swaps the SQL Server database
/// for an EF Core in-memory database. Every factory instance gets its own, empty database
/// (seeded with the sample users/event by DbInitializer), so test classes cannot affect each other.
/// </summary>
public class RaceDayApiFactory : WebApplicationFactory<Program>
{
    // One database name per factory. It is created once here (not inside the AddDbContext lambda)
    // so that every request/scope in the same test class talks to the SAME in-memory database.
    private readonly string _databaseName = $"RaceDayTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server configuration that Program.cs registered.
            var sqlServerRegistrations = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    descriptor.ServiceType == typeof(DbContextOptions) ||
                    (descriptor.ServiceType.IsGenericType &&
                     descriptor.ServiceType.GetGenericTypeDefinition().Name
                         .StartsWith("IDbContextOptionsConfiguration")))
                .ToList();

            foreach (ServiceDescriptor descriptor in sqlServerRegistrations)
            {
                services.Remove(descriptor);
            }

            // Use an in-memory database instead.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Replace Azure Blob Storage with a fake so the tests never need an Azure account.
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IBlobStorageService, FakeBlobStorageService>();
        });
    }
}
