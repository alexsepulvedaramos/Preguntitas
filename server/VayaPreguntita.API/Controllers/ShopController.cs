using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Localization;
using VayaPreguntita.API.Services;

namespace VayaPreguntita.API.Controllers;

// Spending coins (§17.2). Rama 35: streak freeze (own or gift) and streak repair; the
// cosmetics catalog arrives with rama 42.
[ApiController]
[Route("api/shop")]
[Authorize]
public class ShopController(
    IStreakService streakService,
    ICoinService coinService,
    IGroupsService groupsService
) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // POST api/shop/purchase — 409 when coins are short or a limit is reached.
    [HttpPost("purchase")]
    public async Task<ActionResult<PurchaseResponseDto>> Purchase(PurchaseRequestDto dto)
    {
        var userId = CurrentUserId;
        if (!await groupsService.IsUserInGroupAsync(userId, dto.GroupId))
            return Forbid();

        var isGift = dto.TargetUserId is int target && target != userId;
        var result = dto.Item switch
        {
            PurchaseRequestDto.StreakFreeze => await streakService.BuyFreezeAsync(
                dto.GroupId,
                userId,
                dto.TargetUserId ?? userId
            ),
            PurchaseRequestDto.StreakRepair when !isGift => await streakService.RepairAsync(dto.GroupId, userId),
            _ => (StreakPurchaseResult?)null,
        };

        return result switch
        {
            null => BadRequest("Unknown item."),
            StreakPurchaseResult.Success => Ok(
                new PurchaseResponseDto
                {
                    Balance = await coinService.GetBalanceAsync(userId),
                    MyStreak = await streakService.GetMyStreakAsync(dto.GroupId, userId),
                }
            ),
            StreakPurchaseResult.NotEnoughCoins => Conflict(ServerText.NotEnoughCoins),
            StreakPurchaseResult.FreezeLimitReached => Conflict(
                isGift ? ServerText.GiftFreezeLimitReached : ServerText.FreezeLimitReached
            ),
            StreakPurchaseResult.RepairLimitReached => Conflict(ServerText.StreakRepairLimitReached),
            StreakPurchaseResult.NotRepairable => Conflict(ServerText.StreakNotRepairable),
            StreakPurchaseResult.InvalidTarget => BadRequest(ServerText.GiftTargetNotMember),
            _ => StatusCode(500),
        };
    }
}
