using VayaPreguntita.API.DTOs.Auth;

namespace VayaPreguntita.API.DTOs.Questions;

// A tiny DTO just to show who voted, keeping emails and private data safe
public class VoterDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class OptionResultDto
{
    public int Id { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public UserDto? TargetUser { get; set; }
    public List<UserDto> TeamMembers { get; set; } = []; // Deathmatch only
    public int VoteCount { get; set; }
    public List<VoterDto> Voters { get; set; } = [];
    public double Percentage { get; set; }
}
