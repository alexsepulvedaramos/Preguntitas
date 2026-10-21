using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.DTOs.Questions;

namespace VayaPreguntita.API.DTOs.Daily;

// PUT daily/vote and PUT flash/{id}/vote (rama 43): the fresh results (daily) or flash state,
// plus the wallet after paying or using one from the inventory.
public class ChangeVoteResponseDto
{
    public QuestionResultDto? Results { get; set; }
    public FlashStateDto? Flash { get; set; }
    public int Balance { get; set; }
    public InventoryDto Inventory { get; set; } = new();
}
