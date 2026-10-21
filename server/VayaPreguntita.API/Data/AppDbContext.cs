using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Localization;

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
    public DbSet<CoinTransaction> CoinTransactions { get; set; }
    public DbSet<AuthorGuess> AuthorGuesses { get; set; }
    public DbSet<FlashEntry> FlashEntries { get; set; }
    public DbSet<MonthlySummary> MonthlySummaries { get; set; }
    public DbSet<UserInventoryItem> UserInventory { get; set; }
    public DbSet<UserItem> UserItems { get; set; }
    public DbSet<VoteChange> VoteChanges { get; set; }
    public DbSet<Nudge> Nudges { get; set; }
    public DbSet<Reaction> Reactions { get; set; }

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
        modelBuilder.Entity<QuestionTemplate>().HasIndex(t => t.Key).IsUnique();

        // ==========================================
        // CONTENT TRANSLATIONS (rama 24) — language → text maps as JSONB
        // ==========================================
        ConfigureTranslations(modelBuilder.Entity<Pack>().Property(p => p.NameTranslations));
        ConfigureTranslations(modelBuilder.Entity<Pack>().Property(p => p.DescriptionTranslations));
        ConfigureTranslations(modelBuilder.Entity<QuestionTemplate>().Property(t => t.TextTranslations));
        ConfigureTranslations(modelBuilder.Entity<QuestionTemplateOption>().Property(o => o.TextTranslations));
        ConfigureTranslations(modelBuilder.Entity<Question>().Property(q => q.TextTranslations));
        ConfigureTranslations(modelBuilder.Entity<Option>().Property(o => o.TextTranslations));

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
            e.HasIndex(t => t.PreviousTokenHash);
            e.HasIndex(t => new { t.UserId, t.ExpiresAt });
            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // USER — UI / push language (rama 23); existing accounts get Spanish
        // ==========================================
        modelBuilder.Entity<User>()
            .Property(u => u.PreferredLanguage)
            .HasMaxLength(5)
            .HasDefaultValue(AppLanguages.Default);

        // ==========================================
        // COINS (rama 34, §17.2) — balance on User + append-only ledger
        // ==========================================
        modelBuilder.Entity<User>()
            .ToTable(t => t.HasCheckConstraint("CK_Users_Coins_NonNegative", "\"Coins\" >= 0"));

        modelBuilder.Entity<CoinTransaction>(e =>
        {
            // Every reward is paid once per (user, reason, ref) — the tick and the client may retry.
            e.HasIndex(t => new { t.UserId, t.Reason, t.RefId }).IsUnique();
            // Daily-cap sum and the history page.
            e.HasIndex(t => new { t.UserId, t.CreatedAt });
            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Group)
             .WithMany()
             .HasForeignKey(t => t.GroupId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ==========================================
        // AUTHOR GUESS (rama 36, §17.6) — one per member and daily entry
        // ==========================================
        modelBuilder.Entity<AuthorGuess>(e =>
        {
            e.HasKey(g => new { g.DailyEntryId, g.UserId });
            // Member-card stats: a member's guesses in a group.
            e.HasIndex(g => g.UserId);
            e.HasOne(g => g.DailyEntry)
             .WithMany()
             .HasForeignKey(g => g.DailyEntryId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(g => g.User)
             .WithMany()
             .HasForeignKey(g => g.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(g => g.GuessedCreator)
             .WithMany()
             .HasForeignKey(g => g.GuessedCreatorId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ==========================================
        // FLASH ENTRY (rama 44, §17.11) — one per member, group and local day
        // ==========================================
        modelBuilder.Entity<FlashEntry>(e =>
        {
            e.HasIndex(f => new { f.GroupId, f.LaunchedByUserId, f.LocalDate }).IsUnique();
            // daily/current reads the group's open flashes (ClosesAt > now) on every poll.
            e.HasIndex(f => new { f.GroupId, f.ClosesAt });
            e.HasOne(f => f.Group)
             .WithMany()
             .HasForeignKey(f => f.GroupId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.Question)
             .WithMany()
             .HasForeignKey(f => f.QuestionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.LaunchedBy)
             .WithMany()
             .HasForeignKey(f => f.LaunchedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ==========================================
        // CHANGE MY VOTE AND NUDGE (rama 43, §17.10)
        // ==========================================
        modelBuilder.Entity<VoteChange>(e =>
        {
            // Once per member and question.
            e.HasKey(c => new { c.QuestionId, c.UserId });
            e.HasOne(c => c.Question)
             .WithMany()
             .HasForeignKey(c => c.QuestionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.User)
             .WithMany()
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Nudge>(e =>
        {
            e.Property(n => n.Message).HasMaxLength(NudgeRules.MaxMessageLength);
            // "No other nudge for this member in this group in the last 24 h".
            e.HasIndex(n => new { n.GroupId, n.TargetUserId, n.SentAt });
            e.HasOne(n => n.Group)
             .WithMany()
             .HasForeignKey(n => n.GroupId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(n => n.Question)
             .WithMany()
             .HasForeignKey(n => n.QuestionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(n => n.Sender)
             .WithMany()
             .HasForeignKey(n => n.SenderUserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(n => n.Target)
             .WithMany()
             .HasForeignKey(n => n.TargetUserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // REACTIONS (rama 48, §17.15 item 6)
        // ==========================================
        modelBuilder.Entity<Reaction>(e =>
        {
            e.Property(r => r.Emoji).HasMaxLength(ReactionRules.MaxKeyLength);
            // One per person and target. Rows of the other kind have a null key, and nulls
            // never collide in a unique index.
            e.HasIndex(r => new { r.ChatMessageId, r.UserId }).IsUnique();
            e.HasIndex(r => new { r.VoteId, r.UserId }).IsUnique();
            // "Did this author get a reaction push in the last ReactionRules.PushCooldown?"
            e.HasIndex(r => new { r.AuthorUserId, r.NotifiedAt });
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Reactions_OneTarget",
                "(\"ChatMessageId\" IS NULL) <> (\"VoteId\" IS NULL)"));
            e.HasOne(r => r.ChatMessage)
             .WithMany()
             .HasForeignKey(r => r.ChatMessageId)
             .OnDelete(DeleteBehavior.Cascade);
            // Changing your vote (rama 43) replaces the Vote rows: reactions to the old text go.
            e.HasOne(r => r.Vote)
             .WithMany()
             .HasForeignKey(r => r.VoteId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.User)
             .WithMany()
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Author)
             .WithMany()
             .HasForeignKey(r => r.AuthorUserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // ITEM INVENTORY (rama 45, §17.10)
        // ==========================================
        modelBuilder.Entity<UserInventoryItem>(e =>
        {
            e.ToTable("UserInventory", t =>
                t.HasCheckConstraint("CK_UserInventory_Quantity_NonNegative", "\"Quantity\" >= 0"));
            e.HasKey(i => new { i.UserId, i.Item });
            e.HasOne(i => i.User)
             .WithMany()
             .HasForeignKey(i => i.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // SHOP COSMETICS (rama 42, §17.10) — owned once, equipped through User's slots
        // ==========================================
        modelBuilder.Entity<UserItem>(e =>
        {
            e.HasKey(i => new { i.UserId, i.ItemKey });
            e.Property(i => i.ItemKey).HasMaxLength(40);
            e.HasOne(i => i.User)
             .WithMany()
             .HasForeignKey(i => i.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.RingSkinKey).HasMaxLength(40);
            e.Property(u => u.NameColorKey).HasMaxLength(40);
            e.Property(u => u.ThemeKey).HasMaxLength(40);
        });

        // ==========================================
        // MONTHLY SUMMARY (rama 41, §17.12) — one snapshot per group and month
        // ==========================================
        modelBuilder.Entity<MonthlySummary>(e =>
        {
            e.HasIndex(s => new { s.GroupId, s.Month }).IsUnique();
            e.Property(s => s.Payload).HasColumnType("jsonb");
            e.HasOne(s => s.Group)
             .WithMany()
             .HasForeignKey(s => s.GroupId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // A Dictionary<string, string> stored as a JSONB object ({"en": "...", "de": "..."}).
    // Npgsql would map the dictionary to hstore by default, hence the explicit converter.
    private static void ConfigureTranslations(PropertyBuilder<Dictionary<string, string>> property)
    {
        property
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v =>
                    System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                        v,
                        (System.Text.Json.JsonSerializerOptions?)null
                    ) ?? new Dictionary<string, string>(),
                new ValueComparer<Dictionary<string, string>>(
                    (a, b) => (a ?? new()).Count == (b ?? new()).Count && !(a ?? new()).Except(b ?? new()).Any(),
                    v => v.OrderBy(kv => kv.Key).Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key, kv.Value)),
                    v => new Dictionary<string, string>(v)
                )
            );
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
