using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Localization;

namespace VayaPreguntita.API.Controllers;

// The guess errors (§17.6, §17.17), shared by the daily and the flash endpoints.
internal static class GuessErrors
{
    public static ActionResult ToResult(ControllerBase controller, GuessResult result) =>
        result switch
        {
            GuessResult.NoActiveQuestion => controller.NotFound(ServerText.NoActiveQuestionToday),
            GuessResult.NotGuessable => controller.BadRequest(ServerText.GuessNotAvailable),
            GuessResult.MustVoteFirst => controller.BadRequest(ServerText.GuessMustVoteFirst),
            GuessResult.AlreadyGuessed => controller.Conflict(ServerText.AlreadyGuessed),
            GuessResult.InvalidCandidate => controller.BadRequest(ServerText.GuessInvalidCandidate),
            GuessResult.NotEnoughCoins => controller.Conflict(ServerText.NotEnoughCoins),
            GuessResult.HintNotAvailable => controller.BadRequest(ServerText.HintNotAvailable),
            GuessResult.NoneOwned => controller.Conflict(ServerText.NoItemOwned),
            _ => controller.StatusCode(500),
        };
}
