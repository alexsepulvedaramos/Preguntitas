namespace VayaPreguntita.API.Services;

public interface ISeasonalAnnouncementService
{
    // Sends the push for every seasonal item on sale that hasn't been announced yet, if it is
    // announcement time (20:00–23:00 Madrid).
    Task AnnounceDueAsync();
}
