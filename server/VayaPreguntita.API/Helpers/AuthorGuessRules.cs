using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Helpers;

// Rules of "guess the author" (§17.6), kept in one place so the guessable check is easy to
// extend (the backlog's "suggest a pack question" will make suggested clones guessable too).
public static class AuthorGuessRules
{
    // Below this, guessing is trivial (2 members) — at least 2 candidates are needed.
    public const int MinMembers = 3;

    // The member who wrote the question, or null when it isn't member-written (§17.3): pack
    // clones don't count, even when a member edited them.
    public static int? AuthorOf(Question question) =>
        question.Source == QuestionSource.UserCreated ? question.CreatorId : null;

    // Guessable when it's member-written, its author is still a member and the group is big
    // enough to make it a game.
    public static bool IsGuessable(Question question, IReadOnlyCollection<int> memberIds) =>
        AuthorOf(question) is int author
        && memberIds.Contains(author)
        && memberIds.Count >= MinMembers;

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
