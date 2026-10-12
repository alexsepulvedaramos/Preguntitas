using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Localization;
using VayaPreguntita.API.Services;

namespace VayaPreguntita.API.Controllers;

// Spending coins (§17.2). Rama 35: streak freeze (own or gift) and streak repair; rama 45:
// the item inventory (buy ahead, use later in any group); the cosmetics catalog arrives with
// rama 42.
[ApiController]
[Route("api/shop")]
[Authorize]
public class ShopController(
    IStreakService streakService,
    ICoinService coinService,
    IInventoryService inventoryService,
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
                dto.TargetUserId ?? userId,
                dto.UseOwned
            ),
            PurchaseRequestDto.StreakRepair when !isGift => await streakService.RepairAsync(
                dto.GroupId,
                userId,
                dto.UseOwned
            ),
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
                    Inventory = await inventoryService.GetAsync(userId),
                }
            ),
            StreakPurchaseResult.NotEnoughCoins => Conflict(ServerText.NotEnoughCoins),
            StreakPurchaseResult.FreezeLimitReached => Conflict(
                isGift ? ServerText.GiftFreezeLimitReached : ServerText.FreezeLimitReached
            ),
            StreakPurchaseResult.RepairLimitReached => Conflict(ServerText.StreakRepairLimitReached),
            StreakPurchaseResult.NotRepairable => Conflict(ServerText.StreakNotRepairable),
            StreakPurchaseResult.InvalidTarget => BadRequest(ServerText.GiftTargetNotMember),
            StreakPurchaseResult.NoneOwned => Conflict(ServerText.NoItemOwned),
            _ => StatusCode(500),
        };
    }

    // GET api/shop/inventory — the items you hold (rama 45, §17.10).
    [HttpGet("inventory")]
    public async Task<ActionResult<InventoryDto>> GetInventory() =>
        Ok(await inventoryService.GetAsync(CurrentUserId));

    // POST api/shop/inventory — buys one item into your inventory; 409 when coins are short.
    [HttpPost("inventory")]
    public async Task<ActionResult<BuyItemResponseDto>> BuyItem(BuyItemRequestDto dto)
    {
        if (!Enum.IsDefined(dto.Item))
            return BadRequest("Unknown item.");

        var userId = CurrentUserId;
        if (!await inventoryService.BuyAsync(userId, dto.Item))
            return Conflict(ServerText.NotEnoughCoins);

        return Ok(
            new BuyItemResponseDto
            {
                Balance = await coinService.GetBalanceAsync(userId),
                Inventory = await inventoryService.GetAsync(userId),
            }
        );
    }
}
