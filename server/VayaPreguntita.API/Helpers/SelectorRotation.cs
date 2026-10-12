using VayaPreguntita.API.Entities;

namespace VayaPreguntita.API.Helpers;

// Shuffled selector rotation (§17.4): fair — every member is selector exactly once per
// round — but unpredictable, because each turn is drawn at random among the members still
// due in the round. The order is never stored ahead of time. Callers persist the changes.
public static class SelectorRotation
{
    // Draws the next selector and records the turn on the member (and the group, when a
    // new round starts). `previousSelectorId` is excluded across a round boundary when the
    // group has ≥ 3 members, so nobody chooses twice in a row.
    public static GroupMember Draw(Group group, int? previousSelectorId, Random random)
    {
        var members = group.Members;
        var due = members.Where(m => m.LastSelectorRound < group.SelectorRound).ToList();

        if (due.Count == 0)
        {
            group.SelectorRound++;
            due = members
                .Where(m => members.Count < 3 || m.UserId != previousSelectorId)
                .ToList();
        }

        var picked = due[random.Next(due.Count)];
        picked.LastSelectorRound = group.SelectorRound;
        return picked;
    }

    // Spends `member`'s turn in the current round without a draw (bootstrap: the creator
    // is always the first selector, and that counts as their round-1 turn).
    public static void ClaimTurn(Group group, GroupMember member) =>
        member.LastSelectorRound = group.SelectorRound;

    // A member who joins mid-round enters the current round, still due a turn in it.
    public static int RoundForNewMember(Group group) => group.SelectorRound - 1;
}
