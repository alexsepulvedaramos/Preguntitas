using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Helpers;

// Rules of the guess (§17.6, §17.17), kept in one place so the guessable check is easy to
// extend (the backlog's "suggest a pack question" will make suggested clones guessable too).
// One guess per question: who wrote it, who chose it or who launched the flash.
public static class AuthorGuessRules
{
    // Below this, guessing is trivial (2 members) — at least 2 candidates are needed.
    public const int MinMembers = 3;

    // The member who wrote the question, or null when it isn't member-written (§17.3): pack
    // clones don't count, even when a member edited them.
    public static int? AuthorOf(Question question) =>
        question.Source == QuestionSource.UserCreated ? question.CreatorId : null;

    // What there is to guess on a daily entry (Question loaded): its author when member-written,
    // otherwise whoever actively chose it (picked it, or confirmed the automatic proposal; the
    // race winner is the selector by then). An automatic pick nobody touched has nothing.
    public static GuessTarget? TargetOf(DailyEntry entry)
    {
        if (entry.Question.Source == QuestionSource.UserCreated)
            return entry.Question.CreatorId is int author ? new GuessTarget(GuessKind.Author, author) : null;
        return entry.SelectionKind is SelectionKind.Picked or SelectionKind.Confirmed
            ? new GuessTarget(GuessKind.Chooser, entry.SelectorUserId)
            : null;
    }

    // A flash's launcher, when it was launched to be guessed (never on an anonymous one).
    public static GuessTarget? TargetOf(FlashEntry flash) =>
        flash.GuessLauncher && !flash.AnonymousAuthor
            ? new GuessTarget(GuessKind.Launcher, flash.LaunchedByUserId)
            : null;

    // Today's question was the automatic pick and nobody chose it: «Hoy decidió el azar».
    public static bool DecidedByChance(DailyEntry entry) =>
        entry.SelectionKind == SelectionKind.Auto && entry.Question.Source != QuestionSource.UserCreated;

    // Guessable when there is someone to guess, they are still a member and the group is big
    // enough to make it a game.
    public static bool IsGuessable(GuessTarget? target, IReadOnlyCollection<int> memberIds) =>
        target is { } t && memberIds.Contains(t.UserId) && memberIds.Count >= MinMembers;

    // Every current member except the one guessing. Members blacklisted from the question
    // and late joiners stay in as decoys, so the list never reveals when someone joined.
    public static List<int> Candidates(IEnumerable<int> memberIds, int guesserId) =>
        memberIds.Where(id => id != guesserId).OrderBy(id => id).ToList();

    // The hint removes half of the wrong candidates, rounded down; it's only offered when
    // that removes at least one.
    public static int HintRemovals(int wrongCount) => wrongCount / 2;

    // Which wrong candidates the hint hides. Deterministic per (entry, member), so reloading
    // after buying it always shows the same list.
    public static HashSet<int> HiddenByHint(int entryId, int guesserId, IEnumerable<int> wrongIds)
    {
        var shuffled = wrongIds.OrderBy(id => Mix(entryId, guesserId, id)).ThenBy(id => id).ToList();
        return shuffled.Take(HintRemovals(shuffled.Count)).ToHashSet();
    }

    // FNV-1a plus a final avalanche: a stable hash (string.GetHashCode is randomised per process).
    private static uint Mix(int a, int b, int c)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var value in new[] { a, b, c })
            {
                hash ^= (uint)value;
                hash *= 16777619u;
            }
            hash ^= hash >> 15;
            hash *= 0x2c1b3c6du;
            hash ^= hash >> 12;
            return hash;
        }
    }
}

public enum GuessKind
{
    Author,
    Chooser,
    Launcher,
}

// Who a guess is about, and in which role.
public record GuessTarget(GuessKind Kind, int UserId)
{
    // As sent in AuthorGuessDto.Kind.
    public string KindKey =>
        Kind switch
        {
            GuessKind.Chooser => "chooser",
            GuessKind.Launcher => "launcher",
            _ => "author",
        };
}
