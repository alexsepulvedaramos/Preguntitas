using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using VayaPreguntita.API.Data;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;

    public AppDbContext Context { get; private set; }

    // Expose the raw connection string directly from the container to keep the password intact
    public string ConnectionString => _postgres.GetConnectionString();

    public DatabaseFixture()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        Context = new AppDbContext(options);
        await Context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
        await _postgres.StopAsync();
    }
}
