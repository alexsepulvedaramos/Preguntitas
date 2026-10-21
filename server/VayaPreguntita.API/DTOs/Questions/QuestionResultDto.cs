// QuestionResultDto.cs
using VayaPreguntita.API.DTOs.Reactions;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs.Questions;

public class QuestionResultDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public DateTime DateCreated { get; set; }
    public int TotalVotes { get; set; }
    public List<OptionResultDto> Results { get; set; } = [];

    // OpenText answers, and CustomPoll "Otro" free-text answers.
    public List<FreeTextResponseDto> FreeTextResponses { get; set; } = [];

    // History detail: who wrote it (member-written questions only, never for the open one).
    public VoterDto? Author { get; set; }

    // History gating (§17.7): true until the caller votes this question; then Results and
    // FreeTextResponses stay empty and only the text and TotalVotes are shown.
    public bool Locked { get; set; }

    // Locked and closed: the question to vote late from the history (null otherwise).
    public QuestionToVoteDto? LateVoteQuestion { get; set; }

    // Scale only — needed by the frontend to render the full distribution range.
    public int? RangeMin { get; set; }
    public int? RangeMax { get; set; }
}

public class FreeTextResponseDto
{
    // The Vote row carrying the text: what a reaction points at (rama 48).
    public int VoteId { get; set; }
    // Author, so the answer bubble can show their avatar and streak frame (rama 19).
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? FrameColor { get; set; }
    public string Text { get; set; } = string.Empty;
    // Rama 48: who reacted with what.
    public List<ReactionDto> Reactions { get; set; } = [];
}
