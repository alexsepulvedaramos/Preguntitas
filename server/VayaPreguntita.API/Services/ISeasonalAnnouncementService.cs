namespace VayaPreguntita.API.Services;

public interface ISeasonalAnnouncementService
{
    // Sends the push for every seasonal item on sale that hasn't been announced yet, if it is
    // announcement time (16:00–19:00 Madrid).
    Task AnnounceDueAsync();
}
