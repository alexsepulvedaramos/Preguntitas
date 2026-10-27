namespace VayaPreguntita.API.Entities;

// "¿Quién la lanzó?" (§17.17): the guess on a flash whose launcher is kept hidden. One row
// per member and flash.
public class FlashGuess : GuessRow
{
    public int FlashEntryId { get; set; }
    public FlashEntry FlashEntry { get; set; } = null!;
}
