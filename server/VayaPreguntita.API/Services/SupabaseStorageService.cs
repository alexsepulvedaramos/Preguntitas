using Microsoft.Extensions.Options;
using VayaPreguntita.API.Options;

namespace VayaPreguntita.API.Services;

public class SupabaseStorageService(
    HttpClient httpClient,
    IOptions<SupabaseStorageOptions> options)
{
    private readonly SupabaseStorageOptions _opts = options.Value;

    public async Task<string> UploadAvatarAsync(int userId, Stream imageStream, string contentType)
    {
        var objectKey = $"{userId}";
        var uploadUrl = $"{_opts.Url}/storage/v1/object/{_opts.AvatarBucket}/{objectKey}";

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_opts.ServiceKey}");
        httpClient.DefaultRequestHeaders.Add("x-upsert", "true");

        using var content = new StreamContent(imageStream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        var response = await httpClient.PostAsync(uploadUrl, content);
        response.EnsureSuccessStatusCode();

        var cacheBust = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return $"{_opts.Url}/storage/v1/object/public/{_opts.AvatarBucket}/{objectKey}?v={cacheBust}";
    }
}
