using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Localization;

namespace VayaPreguntita.API.Controllers;

// The "change my vote" errors (rama 43), shared by the daily and the flash endpoints.
internal static class VoteChangeErrors
{
    public static ActionResult ToResult(ControllerBase controller, VoteChangeResult result) =>
        result switch
        {
            VoteChangeResult.NoOpenQuestion => controller.NotFound(ServerText.NoActiveQuestionToday),
            VoteChangeResult.FlashNotFound => controller.NotFound(ServerText.FlashNotFound),
            VoteChangeResult.FlashClosed => controller.BadRequest(ServerText.FlashClosed),
            VoteChangeResult.NotVoted => controller.BadRequest(ServerText.VoteChangeNotVoted),
            VoteChangeResult.AlreadyChanged => controller.Conflict(ServerText.VoteAlreadyChanged),
            VoteChangeResult.InvalidVote => controller.BadRequest(ServerText.InvalidVote),
            VoteChangeResult.NotEnoughCoins => controller.Conflict(ServerText.NotEnoughCoins),
            VoteChangeResult.NoneOwned => controller.Conflict(ServerText.NoItemOwned),
            _ => controller.StatusCode(500),
        };
}
