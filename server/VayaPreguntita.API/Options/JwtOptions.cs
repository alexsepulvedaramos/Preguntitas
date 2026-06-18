namespace VayaPreguntita.API.Options;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "VayaPreguntita";
    public string Audience { get; set; } = "VayaPreguntitaClient";
    public string Key { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 1;
    public int RefreshTokenDays { get; set; } = 30;
}
