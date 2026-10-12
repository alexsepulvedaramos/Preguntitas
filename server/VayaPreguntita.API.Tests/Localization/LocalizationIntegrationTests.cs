using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.DTOs.Auth;
using VayaPreguntita.API.DTOs.Users;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Localization;

// Rama 23: the account's language (User.PreferredLanguage) and API messages answered in the
// language the client sends as Accept-Language.
public class LocalizationIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly HttpClient _client;
    private readonly DatabaseFixture _fixture;

    public LocalizationIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = new CustomWebAppFactory(_fixture.ConnectionString).CreateClient();
    }

    private sealed record RegisterBody(string Username, string Email, string Password, string? PreferredLanguage);

    private static RegisterBody NewUser(string? preferredLanguage = null)
    {
        var id = Guid.NewGuid().ToString("N")[..10];
        return new RegisterBody($"u{id}", $"{id}@example.com", "12345678", preferredLanguage);
    }

    private async Task<AuthResponseDto> RegisterAsync(RegisterBody request)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
    }

    [Theory]
    [InlineData("de", "de")]
    [InlineData("en", "en")]
    [InlineData("fr", "es")]
    [InlineData(null, "es")]
    public async Task Register_StoresTheDeviceLanguage_OrSpanish(string? sent, string expected)
    {
        var auth = await RegisterAsync(NewUser(sent));

        auth.PreferredLanguage.Should().Be(expected);
        var stored = await _fixture.Context.Users.AsNoTracking().SingleAsync(u => u.Id == auth.UserId);
        stored.PreferredLanguage.Should().Be(expected);
    }

    [Fact]
    public async Task Login_ReturnsTheAccountLanguage()
    {
        var request = NewUser("en");
        await RegisterAsync(request);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { Identifier = request.Username, Password = request.Password });

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        auth!.PreferredLanguage.Should().Be("en");
    }

    [Theory]
    [InlineData("en", "That username is already taken.")]
    [InlineData("de", "Dieser Benutzername ist schon vergeben.")]
    [InlineData("es", "Ese nombre de usuario ya está en uso.")]
    [InlineData("fr", "Ese nombre de usuario ya está en uso.")]
    public async Task Errors_AnswerInTheRequestLanguage(string acceptLanguage, string expected)
    {
        var request = NewUser();
        await RegisterAsync(request);

        var duplicate = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(request),
        };
        duplicate.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(acceptLanguage));
        var response = await _client.SendAsync(duplicate);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(expected);
    }

    [Fact]
    public async Task UpdateProfile_ChangesTheAccountLanguage()
    {
        var auth = await RegisterAsync(NewUser());
        var update = new HttpRequestMessage(HttpMethod.Put, "/api/users/me")
        {
            Content = JsonContent.Create(new { PreferredLanguage = "de" }),
        };
        update.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await _client.SendAsync(update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        profile!.PreferredLanguage.Should().Be("de");
    }

    [Fact]
    public async Task UpdateProfile_RejectsAnUnsupportedLanguage()
    {
        var auth = await RegisterAsync(NewUser());
        var update = new HttpRequestMessage(HttpMethod.Put, "/api/users/me")
        {
            Content = JsonContent.Create(new { PreferredLanguage = "fr" }),
        };
        update.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        update.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));

        var response = await _client.SendAsync(update);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be("That language isn't available.");
    }
}
