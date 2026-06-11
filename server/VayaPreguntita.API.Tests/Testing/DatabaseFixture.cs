using System;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using VayaPreguntita.API.Data;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;

    public AppDbContext Context { get; private set; }

    public DatabaseFixture()
    {
        // Usa un contenedor PostgreSQL real (puedes cambiar a InMemory si lo prefieres)
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
        await Context.Database.MigrateAsync();   // aplica migraciones existentes
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
        await _postgres.StopAsync();
    }
}
