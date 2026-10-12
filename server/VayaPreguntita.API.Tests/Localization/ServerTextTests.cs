using System.Globalization;
using System.Reflection;
using FluentAssertions;
using VayaPreguntita.API.Localization;

namespace VayaPreguntita.API.Tests.Localization;

public class ServerTextTests
{
    private static T WithUiCulture<T>(string culture, Func<T> read)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            return read();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData("es", "Ya has votado hoy.")]
    [InlineData("en", "You've already voted today.")]
    [InlineData("en-US", "You've already voted today.")]
    [InlineData("de", "Du hast heute schon abgestimmt.")]
    [InlineData("fr", "Ya has votado hoy.")]
    public void RequestMessages_FollowTheUiCulture_AndFallBackToSpanish(string culture, string expected)
    {
        WithUiCulture(culture, () => ServerText.AlreadyVoted).Should().Be(expected);
    }

    [Theory]
    [InlineData("en", "es", "Ya has votado hoy.")]
    [InlineData("es", "en", "You've already voted today.")]
    public void RequestMessages_AreReadWhenUsed_NotCached(string first, string second, string expected)
    {
        WithUiCulture(first, () => ServerText.AlreadyVoted);
        WithUiCulture(second, () => ServerText.AlreadyVoted).Should().Be(expected);
    }

    [Theory]
    [InlineData("es")]
    [InlineData("en")]
    [InlineData("de")]
    public void EveryRequestMessage_HasTextInEveryLanguage(string culture)
    {
        var properties = typeof(ServerText)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(string))
            .ToList();
        properties.Should().NotBeEmpty();

        foreach (var property in properties)
        {
            var text = WithUiCulture(culture, () => (string?)property.GetValue(null));
            text.Should().NotBeNullOrWhiteSpace($"{property.Name} needs a {culture} text");
        }
    }

    [Fact]
    public void PushTexts_UseTheRecipientsLanguage_WhateverTheCurrentCulture()
    {
        var body = WithUiCulture("es", () => ServerText.Push.StreakDangerBody("de", 5, "Los de siempre", "21:00"));

        body.Should().Be("Deine 5-Tage-Serie in Los de siempre endet um 21:00 Uhr. Stimm vorher ab!");
    }

    [Theory]
    [InlineData("es", "🎯 ¡Te toca elegir en Pisito!")]
    [InlineData("en", "🎯 Your turn to pick in Pisito!")]
    [InlineData("de", "🎯 Du bist dran in Pisito!")]
    [InlineData("xx", "🎯 ¡Te toca elegir en Pisito!")]
    public void PushTexts_FallBackToSpanish(string lang, string expected)
    {
        ServerText.Push.SelectorTurnTitle(lang, "Pisito").Should().Be(expected);
    }
}

public class AppLanguagesTests
{
    [Theory]
    [InlineData("es", "es")]
    [InlineData("en", "en")]
    [InlineData("de", "de")]
    [InlineData("en-US", "en")]
    [InlineData("DE", "de")]
    [InlineData("de_AT", "de")]
    [InlineData(" en ", "en")]
    [InlineData("fr", "es")]
    [InlineData("", "es")]
    [InlineData(null, "es")]
    public void Normalize_MapsAnyTagToASupportedLanguage(string? input, string expected)
    {
        AppLanguages.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("es", true)]
    [InlineData("de", true)]
    [InlineData("en-GB", false)]
    [InlineData("fr", false)]
    [InlineData(null, false)]
    public void IsSupported_AcceptsOnlyExactCodes(string? input, bool expected)
    {
        AppLanguages.IsSupported(input).Should().Be(expected);
    }
}
