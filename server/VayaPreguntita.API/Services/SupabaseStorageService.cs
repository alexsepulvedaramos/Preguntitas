using System.Net.Http.Headers;
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
        if (string.IsNullOrWhiteSpace(_opts.Url) || string.IsNullOrWhiteSpace(_opts.ServiceKey))
            throw new InvalidOperationException("Supabase storage is not configured (missing Url or ServiceKey).");

        var objectKey = $"{userId}";
        var uploadUrl = $"{_opts.Url}/storage/v1/object/{_opts.AvatarBucket}/{objectKey}";

        using var content = new StreamContent(imageStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        // Supabase's gateway requires the "apikey" header on every request,
        // independent of the Authorization bearer token.
        request.Headers.Add("apikey", _opts.ServiceKey);
        request.Headers.Add("Authorization", $"Bearer {_opts.ServiceKey}");
        request.Headers.Add("x-upsert", "true");
        request.Content = content;

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Supabase returned {(int)response.StatusCode}: {body}",
                null,
                response.StatusCode);
        }

        var cacheBust = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return $"{_opts.Url}/storage/v1/object/public/{_opts.AvatarBucket}/{objectKey}?v={cacheBust}";
    }
}
