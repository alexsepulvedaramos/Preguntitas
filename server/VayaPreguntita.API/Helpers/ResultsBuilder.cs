// Helpers/ResultsBuilder.cs
namespace VayaPreguntita.API.Helpers;

using AutoMapper;
using VayaPreguntita.API.DTOs.Auth;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

public static class ResultsBuilder
{
    public static QuestionResultDto Build(Question question, List<Vote> votes, IMapper mapper)
    {
        var result = mapper.Map<QuestionResultDto>(question);
        result.TotalVotes = votes.Count;
        if (result.TotalVotes == 0)
            return result;

        switch (question.Type)
        {
            case QuestionType.CustomPoll:
                foreach (var option in question.Options)
                {
                    var optionVotes = votes.Where(v => v.SelectedOptionId == option.Id).ToList();

                    var optionResult = mapper.Map<OptionResultDto>(option);
                    optionResult.VoteCount = optionVotes.Count;
                    optionResult.Percentage = Percent(optionVotes.Count, result.TotalVotes);
                    optionResult.Voters = mapper.Map<List<VoterDto>>(optionVotes);
                    result.Results.Add(optionResult);
                }
                break;

            case QuestionType.Superlative:
            case QuestionType.SecretPairing:
                foreach (var group in votes.GroupBy(v => v.SelectedTargetUserId))
                {
                    var groupVotes = group.ToList();
                    var targetUser = group.First().SelectedTargetUser;
                    result.Results.Add(
                        new OptionResultDto
                        {
                            Id = group.Key ?? 0,
                            DisplayText =
                                group.Key == null ? "Nadie" : targetUser?.Username ?? "Unknown",
                            TargetUser = mapper.Map<UserDto>(targetUser),
                            VoteCount = groupVotes.Count,
                            Percentage = Percent(groupVotes.Count, result.TotalVotes),
                            Voters = mapper.Map<List<VoterDto>>(groupVotes),
                        }
                    );
                }
                break;

            case QuestionType.Scale:
                foreach (
                    var group in votes
                        .Where(v => v.NumericValue.HasValue)
                        .GroupBy(v => v.NumericValue!.Value)
                        .OrderBy(g => g.Key)
                )
                {
                    var groupVotes = group.ToList();
                    result.Results.Add(
                        new OptionResultDto
                        {
                            Id = group.Key,
                            DisplayText = group.Key.ToString(),
                            VoteCount = groupVotes.Count,
                            Percentage = Percent(groupVotes.Count, result.TotalVotes),
                            Voters = mapper.Map<List<VoterDto>>(groupVotes),
                        }
                    );
                }
                break;

            case QuestionType.Deathmatch:
                for (var i = 0; i < question.Metadata.Teams.Count; i++)
                {
                    var team = question.Metadata.Teams[i];
                    var teamVotes = votes
                        .Where(v =>
                            v.SelectedTargetUserId.HasValue
                            && team.Contains(v.SelectedTargetUserId.Value)
                        )
                        .ToList();

                    result.Results.Add(
                        new OptionResultDto
                        {
                            Id = i + 1,
                            DisplayText = $"Equipo {i + 1}",
                            VoteCount = teamVotes.Count,
                            Percentage = Percent(teamVotes.Count, result.TotalVotes),
                            Voters = mapper.Map<List<VoterDto>>(teamVotes),
                        }
                    );
                }
                break;
        }

        return result;
    }

    private static double Percent(int part, int total) =>
        total == 0 ? 0 : Math.Round((double)part / total * 100, 1);
}
