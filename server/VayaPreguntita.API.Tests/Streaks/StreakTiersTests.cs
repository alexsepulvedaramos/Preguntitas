using System.Globalization;
using FluentAssertions;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Tests.Streaks;

// Streak titles, milestones and chat messages in every language (rama 24).
public class StreakTiersTests
{
    [Theory]
    [InlineData("es", "La Vieja del Visillo")]
    [InlineData("en", "The Curtain Twitcher")]
    [InlineData("de", "Die Gardinenspionin")]
    [InlineData("fr", "La Vieja del Visillo")]
    public void TheTopTitle_ReadsInTheRequestLanguage(string language, string expected)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);

        StreakTiers.DisplayedTitle(highestStreakEver: 400, selectedTitleKey: null).Should().Be(expected);
    }

    [Fact]
    public void EveryTierHasATitleAndMaterialInEveryLanguage()
    {
        foreach (var tier in StreakTiers.All)
        foreach (var language in new[] { "es", "en", "de" })
        {
            tier.Titles.In(language).Should().NotBeNullOrWhiteSpace();
            tier.Materials.In(language).Should().NotBeNullOrWhiteSpace();
        }
    }

    [Theory]
    [InlineData("es", "🔥 ana lleva 30 días seguidos · ¡Carne de tertulia!")]
    [InlineData("en", "🔥 ana is on a 30-day streak · Talk-Show Regular!")]
    [InlineData("de", "🔥 ana hat 30 Tage in Folge abgestimmt · Talkshow-Dauergast!")]
    public void ATierChatMessage_IsWrittenInTheReadersLanguage(string language, string expected) =>
        StreakTiers.ChatMessage(language, "ana", 30).Should().Be(expected);

    [Theory]
    [InlineData("es", "🎉 ana lleva 21 días seguidos · ¡3 semanas!")]
    [InlineData("en", "🎉 ana is on a 21-day streak · 3 weeks!")]
    [InlineData("de", "🎉 ana hat 21 Tage in Folge abgestimmt · 3 Wochen!")]
    public void AMilestoneChatMessage_IsWrittenInTheReadersLanguage(string language, string expected) =>
        StreakTiers.ChatMessage(language, "ana", 21).Should().Be(expected);

    [Fact]
    public void AnOrdinaryDay_HasNoChatMessage() => StreakTiers.ChatMessage("en", "ana", 22).Should().BeNull();
}
