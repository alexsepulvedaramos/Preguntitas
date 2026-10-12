using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Helpers;

// What a monthly summary stores (§17.12). Only user ids: names and avatars are read fresh
// when the summary is shown.
public record MonthlySummaryPayload(
    int Questions,
    int Answered,
    int Eligible,
    List<MemberMonth> Members,
    List<MonthlyAward> Awards,
    List<MonthlyMatch> Matches
);

// Answered = daily questions voted on time; Eligible = daily questions open since they
// joined; BestRun = longest run of consecutive eligible questions voted on time.
public record MemberMonth(int UserId, int Answered, int Eligible, int BestRun);

// Awards are titles, not positions: every member tied at the top shares it.
public record MonthlyAward(string Key, int Value, List<AwardWinner> Winners);

// QuestionText is set for the star author only: the question that earned it.
public record AwardWinner(int UserId, string? QuestionText = null);

// A member's "match": whoever they picked most mutually (ties share it).
public record MonthlyMatch(int UserId, List<int> MatchUserIds, int Count);

// Builds the payload from one month's data. Pure, so the rules are easy to test. Everything
// counts daily questions and on-time votes only: flashes and late votes never reach the
// monthly awards (§17.7, §17.11). Only current members are counted.
public static class MonthlySummaryBuilder
{
    public static class AwardKeys
    {
        public const string Superlative = "superlative";
        public const string Selector = "selector";
        public const string Guesser = "guesser";
        public const string Consistent = "consistent";
        public const string StarAuthor = "starAuthor";
    }

    // Below this many eligible questions, a member can't win "most consistent": joining
    // on the 29th and voting twice isn't 100 % consistency.
    public const int MinEligibleForConsistency = 5;

    public record EntryData(
        int EntryId,
        DateOnly Date,
        DateTime ActivatedAt,
        int QuestionId,
        QuestionType Type,
        string QuestionText,
        int? AuthorId,
        int SelectorUserId,
        SelectionKind SelectionKind
    );

    public record MemberData(int UserId, DateTime JoinedAt);

    // One on-time vote row (multi-row votes such as Secret Pairing come as several rows).
    public record VoteData(int QuestionId, int UserId, int? TargetUserId);

    public static MonthlySummaryPayload Build(
        IReadOnlyCollection<EntryData> entries,
        IReadOnlyCollection<MemberData> members,
        IReadOnlyCollection<VoteData> votes,
        IReadOnlyCollection<(int EntryId, int UserId)> correctGuesses
    )
    {
        var ordered = entries.OrderBy(e => e.Date).ToList();
        var memberIds = members.Select(m => m.UserId).ToHashSet();
        var votedBy = votes
            .GroupBy(v => v.QuestionId)
            .ToDictionary(g => g.Key, g => g.Select(v => v.UserId).ToHashSet());

        var memberMonths = members
            .Select(m => BuildMember(m, ordered, votedBy))
            .OrderBy(m => m.UserId)
            .ToList();

        var awards = new List<MonthlyAward>();
        AddAward(awards, AwardKeys.Superlative, CountBy(
            from v in votes
            join e in ordered on v.QuestionId equals e.QuestionId
            where e.Type == QuestionType.Superlative
                && v.TargetUserId is int t && memberIds.Contains(t) && memberIds.Contains(v.UserId)
            select v.TargetUserId!.Value));
        AddAward(awards, AwardKeys.Selector, CountBy(
            ordered.Where(e => e.SelectionKind != SelectionKind.Auto && memberIds.Contains(e.SelectorUserId))
                .Select(e => e.SelectorUserId)));
        AddAward(awards, AwardKeys.Guesser, CountBy(
            correctGuesses.Where(g => memberIds.Contains(g.UserId)).Select(g => g.UserId)));
        AddAward(awards, AwardKeys.Consistent, memberMonths
            .Where(m => m.Eligible >= MinEligibleForConsistency && m.Answered > 0)
            .ToDictionary(m => m.UserId, m => Percent(m.Answered, m.Eligible)));
        if (StarAuthor(ordered, memberIds, votedBy) is { } star)
            awards.Add(star);

        return new MonthlySummaryPayload(
            ordered.Count,
            memberMonths.Sum(m => m.Answered),
            memberMonths.Sum(m => m.Eligible),
            memberMonths,
            awards,
            Matches(ordered, memberIds, votes)
        );
    }

    public static int Percent(int part, int total) =>
        total == 0 ? 0 : (int)Math.Round(100.0 * part / total, MidpointRounding.AwayFromZero);

    private static MemberMonth BuildMember(
        MemberData member,
        List<EntryData> ordered,
        Dictionary<int, HashSet<int>> votedBy)
    {
        int answered = 0, eligible = 0, run = 0, bestRun = 0;
        foreach (var entry in ordered)
        {
            var voted = votedBy.TryGetValue(entry.QuestionId, out var voters) && voters.Contains(member.UserId);
            // Open since they joined: it was still open (24 h) when they arrived.
            if (!voted && entry.ActivatedAt.AddDays(1) <= member.JoinedAt)
                continue;

            eligible++;
            if (voted)
            {
                answered++;
                bestRun = Math.Max(bestRun, ++run);
            }
            else
            {
                run = 0;
            }
        }
        return new MemberMonth(member.UserId, answered, eligible, bestRun);
    }

    private static Dictionary<int, int> CountBy(IEnumerable<int> userIds) =>
        userIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

    // The members with the highest value share the award; nobody wins with 0.
    private static void AddAward(List<MonthlyAward> awards, string key, Dictionary<int, int> values)
    {
        if (values.Count == 0)
            return;
        var top = values.Values.Max();
        if (top <= 0)
            return;
        var winners = values.Where(kv => kv.Value == top)
            .OrderBy(kv => kv.Key)
            .Select(kv => new AwardWinner(kv.Key))
            .ToList();
        awards.Add(new MonthlyAward(key, top, winners));
    }

    // The member-written question with the most on-time answers; ties go to whoever had more
    // member-written questions played that month, and a remaining tie is shared.
    private static MonthlyAward? StarAuthor(
        List<EntryData> ordered,
        HashSet<int> memberIds,
        Dictionary<int, HashSet<int>> votedBy)
    {
        var authored = ordered
            .Where(e => e.AuthorId is int a && memberIds.Contains(a))
            .Select(e => new
            {
                Entry = e,
                Author = e.AuthorId!.Value,
                Answers = votedBy.TryGetValue(e.QuestionId, out var v) ? v.Count : 0,
            })
            .ToList();
        if (authored.Count == 0)
            return null;

        var topAnswers = authored.Max(a => a.Answers);
        if (topAnswers <= 0)
            return null;

        var playedBy = authored.GroupBy(a => a.Author).ToDictionary(g => g.Key, g => g.Count());
        var contenders = authored.Where(a => a.Answers == topAnswers).ToList();
        var mostPlayed = contenders.Max(a => playedBy[a.Author]);

        var winners = contenders
            .Where(a => playedBy[a.Author] == mostPlayed)
            .GroupBy(a => a.Author)
            .OrderBy(g => g.Key)
            .Select(g => new AwardWinner(g.Key, g.First().Entry.QuestionText))
            .ToList();
        return new MonthlyAward(AwardKeys.StarAuthor, topAnswers, winners);
    }

    // "Tu match": picks in Superlatives and Secret Pairings, counted both ways. A pair only
    // counts when each picked the other at least once.
    private static List<MonthlyMatch> Matches(
        List<EntryData> ordered,
        HashSet<int> memberIds,
        IReadOnlyCollection<VoteData> votes)
    {
        var pickTypes = ordered
            .Where(e => e.Type is QuestionType.Superlative or QuestionType.SecretPairing)
            .Select(e => e.QuestionId)
            .ToHashSet();
        var picks = votes
            .Where(v => pickTypes.Contains(v.QuestionId)
                && v.TargetUserId is int t && t != v.UserId
                && memberIds.Contains(v.UserId) && memberIds.Contains(t))
            .GroupBy(v => (From: v.UserId, To: v.TargetUserId!.Value))
            .ToDictionary(g => g.Key, g => g.Count());

        var scores = new Dictionary<int, List<(int Other, int Score)>>();
        foreach (var ((from, to), count) in picks)
        {
            if (from > to || !picks.TryGetValue((to, from), out var back))
                continue;
            var score = count + back;
            scores.TryAdd(from, []);
            scores.TryAdd(to, []);
            scores[from].Add((to, score));
            scores[to].Add((from, score));
        }

        return scores
            .OrderBy(kv => kv.Key)
            .Select(kv =>
            {
                var best = kv.Value.Max(p => p.Score);
                var others = kv.Value.Where(p => p.Score == best).Select(p => p.Other).OrderBy(id => id).ToList();
                return new MonthlyMatch(kv.Key, others, best);
            })
            .ToList();
    }
}
