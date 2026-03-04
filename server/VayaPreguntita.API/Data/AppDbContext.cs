// DbContext for the application, using Entity Framework Core
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Entities;

namespace VayaPreguntita.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Question> Questions { get; set; }
    public DbSet<Answer> Answers { get; set; }
}
