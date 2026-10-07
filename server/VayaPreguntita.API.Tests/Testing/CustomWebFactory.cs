using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VayaPreguntita.API.Data;

namespace VayaPreguntita.API.Tests.Testing;

public class CustomWebAppFactory(string connectionString) : WebApplicationFactory<Program>
{
    private readonly string _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program.cs reads these while building the host, so they must be settings (not test services)
        builder.UseSetting("Jwt:Key", "test-only-signing-key-at-least-32-bytes-long!!");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "100000");

        builder.ConfigureTestServices(services =>
        {
            // 1. Find the default database configuration from the main application
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>)
            );

            // EF Core 9+ also keeps the original UseNpgsql(...) callback in this registration
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            // 2. Remove the default configuration to prevent connecting to the development database
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 3. Inject the Testcontainers PostgreSQL connection string
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_connectionString);
            });
        });
    }
}
