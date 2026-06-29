namespace VayaPreguntita.API.DTOs.Notifications;

public record NotificationPreferencesDto(
    bool NewQuestion,
    bool SelectorTurn,
    bool UserVoted,
    bool NewMessage
);
