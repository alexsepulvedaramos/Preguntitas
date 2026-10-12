using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VayaPreguntita.API.BackgroundServices;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Services;

namespace VayaPreguntita.API.Tests.Testing;

// Base for tests that run the domain services against a real PostgreSQL (Testcontainers).
// Every test starts from an empty database, at an instant pinned on the TestClock, with
// push notifications mocked out. Concrete classes need [Collection(PostgresCollection.Name)].
public abstract class DomainTestBase(DatabaseFixture fixture) : IAsyncLifetime
{
    // Every seeded group asks its question at 21:00 (UTC+2) and was created on 2026-10-01.
    // Its members are all still due a turn in selector round 1 (§17.4).
    protected static readonly TimeOnly DailyTime = new(21, 0);
    protected static readonly DateTime GroupCreatedUtc = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    protected TestClock Clock { get; } = new();
    protected Mock<INotificationService> Notifications { get; } = new();

    public async Task InitializeAsync()
    {
        await using var db = NewContext();
        var tables = db
            .Model.GetEntityTypes()
            .Where(t => !t.IsOwned())
            .Select(t => t.GetTableName())
            .OfType<string>()
            .Distinct()
            .Select(name => $"\"{name}\"");
        await db.Database.ExecuteSqlRawAsync(
            $"TRUNCATE TABLE {string.Join(", ", tables)} RESTART IDENTITY CASCADE"
        );
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options);

    // ── Services wired as in production, minus the push client ──────────────

    protected StreakService NewStreakService(AppDbContext db) =>
        new(
            db,
            Notifications.Object,
            NewCoinService(db),
            NewInventoryService(db),
            Clock,
            NullLogger<StreakService>.Instance
        );

    protected CoinService NewCoinService(AppDbContext db) => new(db, Clock);

    protected InventoryService NewInventoryService(AppDbContext db) => new(db, NewCoinService(db));

    protected AuthorGuessService NewAuthorGuessService(AppDbContext db) => new(db, NewCoinService(db), NewInventoryService(db), Clock);

    protected FlashService NewFlashService(AppDbContext db) =>
        new(db, TestMapper.Instance, NewCoinService(db), NewInventoryService(db), Notifications.Object, Clock);

    protected DailyService NewDailyService(AppDbContext db) =>
        new(
            db,
            TestMapper.Instance,
            Notifications.Object,
            NewStreakService(db),
            NewCoinService(db),
            NewAuthorGuessService(db),
            NewFlashService(db),
            Clock
        );

    protected DailyCycleRunner NewCycleRunner(AppDbContext db) =>
        new(
            db,
            NewDailyService(db),
            Notifications.Object,
            NewStreakService(db),
            NewCoinService(db),
            Clock,
            NullLogger<DailyCycleRunner>.Instance
        );

    protected GroupsService NewGroupsService(AppDbContext db) =>
        new(db, TestMapper.Instance, NewDailyService(db), NewStreakService(db), Clock);

    // Runs one background-service tick at the clock's current instant.
    protected async Task TickAsync()
    {
        await using var db = NewContext();
        await NewCycleRunner(db).RunOnceAsync();
    }

    // ── Seeding ──────────────────────────────────────────────────────────────

    protected sealed record SeededGroup(int Id, IReadOnlyDictionary<string, int> UserIds)
    {
        public int this[string username] => UserIds[username];
    }

    // Users join in the order given, one minute apart; the first one is the admin.
    protected async Task<SeededGroup> SeedGroupAsync(params string[] usernames)
    {
        await using var db = NewContext();
        var users = usernames
            .Select(name => new User { Username = name, Email = $"{name}@test.dev" })
            .ToList();
        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        var group = new Group
        {
            Name = "Los de siempre",
            InvitationCode = $"G{Guid.NewGuid():N}"[..6],
            DailyQuestionTime = DailyTime,
            DateCreated = GroupCreatedUtc,
            CreatorId = users[0].Id,
            Members = users
                .Select((u, i) => new GroupMember
                {
                    UserId = u.Id,
                    JoinedAt = GroupCreatedUtc.AddMinutes(i),
                    IsAdmin = i == 0,
                })
                .ToList(),
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        return new SeededGroup(group.Id, users.ToDictionary(u => u.Username, u => u.Id));
    }

    protected async Task<int> AddQuestionAsync(
        int groupId,
        QuestionType type = QuestionType.Superlative,
        string text = "¿Quién llegaría tarde a su propia boda?",
        int? creatorId = null,
        bool isUsed = false,
        Action<Question>? configure = null
    )
    {
        await using var db = NewContext();
        var question = new Question
        {
            GroupId = groupId,
            Type = type,
            Text = text,
            CreatorId = creatorId,
            Source = QuestionSource.UserCreated,
            IsUsed = isUsed,
        };
        configure?.Invoke(question);
        db.Questions.Add(question);
        await db.SaveChangesAsync();
        return question.Id;
    }

    // An activated entry opened at date@T; a pending one was preselected at (date-1)@T.
    protected async Task<int> AddEntryAsync(
        int groupId,
        DateOnly date,
        int questionId,
        int selectorUserId,
        bool activated,
        SelectionKind selectionKind = SelectionKind.Auto
    )
    {
        await using var db = NewContext();
        var activatedAt = activated ? DailyClock.ToUtc(date, DailyTime) : (DateTime?)null;
        var entry = new DailyEntry
        {
            GroupId = groupId,
            Date = date,
            QuestionId = questionId,
            SelectorUserId = selectorUserId,
            SelectionKind = selectionKind,
            PreselectedAt = DailyClock.ToUtc(date.AddDays(-1), DailyTime),
            ActivatedAt = activatedAt,
        };
        db.DailyEntries.Add(entry);

        var question = await db.Questions.SingleAsync(q => q.Id == questionId);
        question.IsUsed = true;
        question.DateActivated = activatedAt;

        await db.SaveChangesAsync();
        return entry.Id;
    }

    // Shortcut: a fresh Superlative question that opened on `date` at T.
    protected async Task<(int EntryId, int QuestionId)> AddActivatedDayAsync(
        int groupId,
        DateOnly date,
        int selectorUserId
    )
    {
        var questionId = await AddQuestionAsync(groupId, text: $"Pregunta del {date:dd/MM}", isUsed: true);
        var entryId = await AddEntryAsync(groupId, date, questionId, selectorUserId, activated: true);
        return (entryId, questionId);
    }

    protected async Task<int> AddPackAsync(string name, bool activeByDefault = true)
    {
        await using var db = NewContext();
        var pack = new Pack { Name = name, IsActiveByDefault = activeByDefault };
        db.Packs.Add(pack);
        await db.SaveChangesAsync();
        return pack.Id;
    }

    protected async Task<int> AddTemplateAsync(int packId, string text, bool retired = false)
    {
        await using var db = NewContext();
        var template = new QuestionTemplate
        {
            PackId = packId,
            Key = $"t-{Guid.NewGuid():N}",
            Text = text,
            Type = QuestionType.Superlative,
            IsRetired = retired,
        };
        db.QuestionTemplates.Add(template);
        await db.SaveChangesAsync();
        return template.Id;
    }
}
