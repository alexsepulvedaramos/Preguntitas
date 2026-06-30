using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
    public DbSet<Pack> Packs { get; set; }
    public DbSet<QuestionTemplate> QuestionTemplates { get; set; }
    public DbSet<GroupDisabledPack> GroupDisabledPacks { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<DevicePushSubscription> DevicePushSubscriptions { get; set; }
    public DbSet<NotificationPreferences> NotificationPreferences { get; set; }
    public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }

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

        // ==========================================
        // QUESTION / QUESTION TEMPLATE — Metadata como JSONB
        // (misma configuración del owned type, reutilizada)
        // ==========================================
        modelBuilder.Entity<Question>().OwnsOne(q => q.Metadata, ConfigureMetadata);
        modelBuilder.Entity<QuestionTemplate>().OwnsOne(t => t.Metadata, ConfigureMetadata);

        // ==========================================
        // QUESTION → QUESTION TEMPLATE (optional back-link for pack clones)
        // ==========================================
        modelBuilder
            .Entity<Question>()
            .HasOne(q => q.Template)
            .WithMany()
            .HasForeignKey(q => q.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        // ==========================================
        // PACK / QUESTION TEMPLATE — relaciones
        // ==========================================
        modelBuilder
            .Entity<QuestionTemplate>()
            .HasOne(t => t.Pack)
            .WithMany(p => p.Templates)
            .HasForeignKey(t => t.PackId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<QuestionTemplateOption>()
            .HasOne(o => o.QuestionTemplate)
            .WithMany(t => t.Options)
            .HasForeignKey(o => o.QuestionTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==========================================
        // GROUP DISABLED PACK (clave compuesta) — per-group pack toggle
        // ==========================================
        modelBuilder.Entity<GroupDisabledPack>().HasKey(gdp => new { gdp.GroupId, gdp.PackId });

        modelBuilder
            .Entity<GroupDisabledPack>()
            .HasOne(gdp => gdp.Group)
            .WithMany()
            .HasForeignKey(gdp => gdp.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<GroupDisabledPack>()
            .HasOne(gdp => gdp.Pack)
            .WithMany()
            .HasForeignKey(gdp => gdp.PackId)
            .OnDelete(DeleteBehavior.Cascade);

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
        // CHAT MESSAGE — cascade from DailyEntry, restrict from User
        // ==========================================
        modelBuilder
            .Entity<ChatMessage>()
            .HasOne(m => m.DailyEntry)
            .WithMany()
            .HasForeignKey(m => m.DailyEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<ChatMessage>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChatMessage>().HasIndex(m => new { m.DailyEntryId, m.UserId });

        // ==========================================
        // VOTE — one-vote-per-question is enforced in DailyService.VoteAsync
        // (not here): SecretPairing and multi-select CustomPoll legitimately
        // write more than one Vote row per (QuestionId, UserId).
        // ==========================================
        modelBuilder.Entity<Vote>().HasIndex(v => new { v.QuestionId, v.UserId });

        // ==========================================
        // NOTIFICATION PREFERENCES — 1:1 with User, PK = UserId
        // ==========================================
        modelBuilder.Entity<NotificationPreferences>().HasKey(np => np.UserId);
        modelBuilder
            .Entity<NotificationPreferences>()
            .HasOne(np => np.User)
            .WithOne()
            .HasForeignKey<NotificationPreferences>(np => np.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==========================================
        // DEVICE PUSH SUBSCRIPTIONS
        // ==========================================
        modelBuilder.Entity<DevicePushSubscription>().HasIndex(s => s.Endpoint).IsUnique();
        modelBuilder
            .Entity<DevicePushSubscription>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<Vote>()
            .HasOne(v => v.SelectedTargetUser)
            .WithMany()
            .HasForeignKey(v => v.SelectedTargetUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // USER REFRESH TOKENS — one row per device/session
        // ==========================================
        modelBuilder.Entity<UserRefreshToken>(e =>
        {
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasIndex(t => new { t.UserId, t.ExpiresAt });
            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // Shared owned-type config for QuestionMetadata (JSONB), used by both
    // Question and QuestionTemplate. Teams (List<List<int>>) needs an explicit
    // converter + value comparer.
    private static void ConfigureMetadata<TOwner>(
        OwnedNavigationBuilder<TOwner, QuestionMetadata> builder
    )
        where TOwner : class
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
                                (x, y) => (x ?? new()).SequenceEqual(y ?? new())
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
}
