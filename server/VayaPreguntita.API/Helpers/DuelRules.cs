using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Helpers;

public enum DuelOutcome
{
    Won,
    Drawn,
    Lost,
}

// Free duels (§17.15 item 6, rama 47). A duel is a Deathmatch of one member against another
// that someone chose on purpose: launched as a flash, or actively chosen by the selector as
// the daily question (anything but SelectionKind.Auto). Nothing is stored for it: whether a
// question is a duel, and how it ended, is read from the question, its entry and its votes.
public static class DuelRules
{
    // The two duelists, or null when the question isn't a 1-vs-1 Deathmatch.
    public static (int First, int Second)? Duelists(Question question)
    {
        if (question.Type != QuestionType.Deathmatch)
            return null;
        var teams = question.Metadata.Teams;
        if (teams.Count != 2 || teams[0].Count != 1 || teams[1].Count != 1 || teams[0][0] == teams[1][0])
            return null;
        return (teams[0][0], teams[1][0]);
    }

    public static bool IsDailyDuel(DailyEntry entry, Question question) =>
        entry.SelectionKind != SelectionKind.Auto && Duelists(question) != null;

    // The flash itself is the deliberate choice.
    public static bool IsFlashDuel(Question question) => Duelists(question) != null;

    // How the duel ended for one duelist given each side's vote count (a 1-vs-1 Deathmatch vote
    // stores the chosen side's member in SelectedTargetUserId). The most-voted side wins, equal
    // votes are a draw, and a duel nobody voted on doesn't count (null). Late votes count too.
    public static DuelOutcome? Outcome(int ownVotes, int opponentVotes)
    {
        if (ownVotes + opponentVotes == 0)
            return null;
        return ownVotes > opponentVotes ? DuelOutcome.Won
            : ownVotes < opponentVotes ? DuelOutcome.Lost
            : DuelOutcome.Drawn;
    }
}
