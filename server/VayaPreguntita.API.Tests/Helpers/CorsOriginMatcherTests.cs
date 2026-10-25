using FluentAssertions;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Tests.Helpers;

// Which browser origins the API accepts: production exactly, plus our own Vercel previews.
public class CorsOriginMatcherTests
{
    private static readonly CorsOriginMatcher Matcher = new(
        ["https://vayapreguntita.vercel.app"],
        ["https://preguntitas-*-alexsepulvedaramos-5307s-projects.vercel.app"]
    );

    [Theory]
    [InlineData("https://vayapreguntita.vercel.app")]
    [InlineData("https://vayapreguntita.vercel.app/")]
    [InlineData("https://preguntitas-git-feat-f-6e0d77-alexsepulvedaramos-5307s-projects.vercel.app")]
    [InlineData("https://preguntitas-dxfafobzk-alexsepulvedaramos-5307s-projects.vercel.app")]
    [InlineData("HTTPS://Preguntitas-Git-Main-AlexSepulvedaRamos-5307s-Projects.vercel.app")]
    public void Accepts_production_and_our_previews(string origin) =>
        Matcher.IsAllowed(origin).Should().BeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://vayapreguntita.vercel.app")]
    [InlineData("https://evil.vercel.app")]
    [InlineData("https://preguntitas--alexsepulvedaramos-5307s-projects.vercel.app")]
    [InlineData("https://preguntitas-git-x-otherteam-projects.vercel.app")]
    [InlineData("https://preguntitas-evil.com.alexsepulvedaramos-5307s-projects.vercel.app")]
    [InlineData("https://preguntitas-x-alexsepulvedaramos-5307s-projects.vercel.app.evil.com")]
    [InlineData("https://preguntitas-x-alexsepulvedaramos-5307s-projects.vercel.app:8443")]
    public void Rejects_anything_else(string? origin) =>
        Matcher.IsAllowed(origin).Should().BeFalse();

    [Fact]
    public void Without_config_allows_nothing()
    {
        var empty = new CorsOriginMatcher(null, null);
        empty.HasAny.Should().BeFalse();
        empty.IsAllowed("https://vayapreguntita.vercel.app").Should().BeFalse();
    }
}
