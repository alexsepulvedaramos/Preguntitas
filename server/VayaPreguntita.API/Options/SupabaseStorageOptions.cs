namespace VayaPreguntita.API.Options;

public sealed class SupabaseStorageOptions
{
    public string Url { get; set; } = "";
    public string ServiceKey { get; set; } = "";
    public string AvatarBucket { get; set; } = "avatars";
}
