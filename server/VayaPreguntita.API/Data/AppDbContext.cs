// DbContext for the application, using Entity Framework Core
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Entities;

namespace VayaPreguntita.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // ==========================================
    // DATABASE TABLES (DbSets)
    // ==========================================
    public DbSet<User> Users { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Option> Options { get; set; }
    public DbSet<Vote> Votes { get; set; }
}
