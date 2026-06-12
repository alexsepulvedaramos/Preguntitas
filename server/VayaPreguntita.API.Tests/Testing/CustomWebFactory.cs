namespace VayaPreguntita.API.Tests.Testing;

public class CustomWebAppFactory(string connectionString) : WebApplicationFactory<Program>
{
    private readonly string _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // 1. Find the default database configuration from the main application
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>)
            );

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
