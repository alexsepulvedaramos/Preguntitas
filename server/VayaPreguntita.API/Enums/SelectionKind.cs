namespace VayaPreguntita.API.Enums;

// How the selector settled a DailyEntry's question (§17.4). Everything but Auto is an
// active choice; Created means the selector wrote the question inline during their turn.
public enum SelectionKind
{
    Auto = 0,
    Confirmed = 1,
    Picked = 2,
    Created = 3,
}
