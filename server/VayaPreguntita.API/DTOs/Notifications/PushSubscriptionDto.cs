namespace VayaPreguntita.API.DTOs.Notifications;

public record PushSubscriptionDto(string Endpoint, string P256dh, string Auth);
