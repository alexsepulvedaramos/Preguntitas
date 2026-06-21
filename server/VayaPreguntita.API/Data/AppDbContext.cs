using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using VayaPreguntita.API.Entities;

namespace VayaPreguntita.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // ==========================================
    // DATABASE TABLES (DbSets)
    // ==========================================
    public DbSet<User> Users { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Option> Options { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<DailyEntry> DailyEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==========================================
        // GROUP MEMBER (clave compuesta)
        // ==========================================
        modelBuilder.Entity<GroupMember>().HasKey(gm => new { gm.GroupId, gm.UserId });

        modelBuilder
            .Entity<GroupMember>()
            .HasOne(gm => gm.Group)
            .WithMany(g => g.Members)
            .HasForeignKey(gm => gm.GroupId);

        modelBuilder
            .Entity<GroupMember>()
            .HasOne(gm => gm.User)
            .WithMany(u => u.GroupMemberships)
            .HasForeignKey(gm => gm.UserId);

        // ==========================================
        // GROUP — evitar ciclos en Creator y Admin
        // ==========================================
        modelBuilder
            .Entity<Group>()
            .HasOne(g => g.Creator)
            .WithMany(u => u.CreatedGroups)
            .HasForeignKey(g => g.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<Group>()
            .HasOne(g => g.Admin)
            .WithMany(u => u.AdministeredGroups)
            .HasForeignKey(g => g.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // QUESTION — Metadata como JSONB
        // ==========================================
        modelBuilder
            .Entity<Question>()
            .OwnsOne(
                q => q.Metadata,
                builder =>
                {
                    builder.ToJson();
                    builder
                        .Property(m => m.Teams)
                        .HasConversion(
                            v =>
                                System.Text.Json.JsonSerializer.Serialize(
                                    v,
                                    (System.Text.Json.JsonSerializerOptions?)null
                                ),
                            v =>
                                System.Text.Json.JsonSerializer.Deserialize<List<List<int>>>(
                                    v,
                                    (System.Text.Json.JsonSerializerOptions?)null
                                ) ?? new List<List<int>>(),
                            new ValueComparer<List<List<int>>>(
                                (a, b) =>
                                    (a ?? new()).SequenceEqual(
                                        b ?? new(),
                                        EqualityComparer<List<int>>.Create(
                                            (x, y) =>
                                                (x ?? new()).SequenceEqual(y ?? new())
                                        )
                                    ),
                                v =>
                                    v.Aggregate(
                                        0,
                                        (hash, inner) =>
                                            HashCode.Combine(hash, inner.Aggregate(0, HashCode.Combine))
                                    ),
                                v => v.Select(inner => inner.ToList()).ToList()
                            )
                        );
                }
            );

        // ==========================================
        // DAILY ENTRY — índice único por grupo+fecha
        // ==========================================
        modelBuilder.Entity<DailyEntry>().HasIndex(d => new { d.GroupId, d.Date }).IsUnique();

        modelBuilder
            .Entity<DailyEntry>()
            .HasOne(d => d.Group)
            .WithMany(g => g.DailyEntries)
            .HasForeignKey(d => d.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<DailyEntry>()
            .HasOne(d => d.Question)
            .WithMany()
            .HasForeignKey(d => d.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<DailyEntry>()
            .HasOne(d => d.Selector)
            .WithMany()
            .HasForeignKey(d => d.SelectorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // VOTE - a user can only vote once per question
        // ==========================================
        modelBuilder.Entity<Vote>().HasIndex(v => new { v.QuestionId, v.UserId }).IsUnique();

        modelBuilder
            .Entity<Vote>()
            .HasOne(v => v.SelectedTargetUser)
            .WithMany()
            .HasForeignKey(v => v.SelectedTargetUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
