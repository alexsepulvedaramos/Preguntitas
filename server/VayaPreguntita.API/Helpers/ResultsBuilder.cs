// Helpers/ResultsBuilder.cs
namespace VayaPreguntita.API.Helpers;

using AutoMapper;
using VayaPreguntita.API.DTOs.Auth;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

public static class ResultsBuilder
{
    public static QuestionResultDto Build(
        Question question,
        List<Vote> votes,
        IMapper mapper,
        IReadOnlyDictionary<int, User>? usersById = null
    )
    {
        var result = mapper.Map<QuestionResultDto>(question);
        result.TotalVotes = votes.Select(v => v.UserId).Distinct().Count();

        if (question.Type == QuestionType.Scale)
        {
            result.RangeMin = question.Metadata.RangeMin;
            result.RangeMax = question.Metadata.RangeMax;
        }

        if (votes.Count == 0)
            return result;

        switch (question.Type)
        {
            case QuestionType.CustomPoll:
                foreach (var option in question.Options)
                {
                    var optionVotes = votes.Where(v => v.SelectedOptionId == option.Id).ToList();
                    if (optionVotes.Count == 0)
                        continue; // Don't clutter the results with unvoted options.

                    var optionResult = mapper.Map<OptionResultDto>(option);
                    optionResult.VoteCount = optionVotes.Count;
                    optionResult.Percentage = Percent(optionVotes.Count, result.TotalVotes);
                    optionResult.Voters = mapper.Map<List<VoterDto>>(optionVotes);
                    result.Results.Add(optionResult);
                }
                result.Results = result.Results.OrderByDescending(r => r.VoteCount).ToList();
                break;

            case QuestionType.Superlative:
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
                result.Results = result.Results.OrderByDescending(r => r.VoteCount).ToList();
                break;

            case QuestionType.SecretPairing:
                // Each voter contributes 2 Vote rows (one per predicted partner) sharing their
                // UserId. Group those back into the pair they predicted (the unordered set of
                // the 2 target ids) so "Naiara + Fockhaman" shows as one combined result, not
                // two separate 50% rows.
                var pairsByVoter = votes
                    .GroupBy(v => v.UserId)
                    .Select(voterVotes => new
                    {
                        FirstRow = voterVotes.First(),
                        PairKey = string.Join(
                            "-",
                            voterVotes
                                .Select(v => v.SelectedTargetUserId!.Value)
                                .OrderBy(id => id)
                        ),
                        TargetNames = voterVotes
                            .Select(v => v.SelectedTargetUser?.Username ?? "Unknown")
                            .OrderBy(name => name)
                            .ToList(),
                    });

                var pairId = 0;
                foreach (
                    var pairGroup in pairsByVoter
                        .GroupBy(v => v.PairKey)
                        .OrderByDescending(g => g.Count())
                )
                {
                    var voters = pairGroup.ToList();
                    result.Results.Add(
                        new OptionResultDto
                        {
                            Id = ++pairId,
                            DisplayText = string.Join(" + ", voters.First().TargetNames),
                            VoteCount = voters.Count,
                            Percentage = Percent(voters.Count, result.TotalVotes),
                            Voters = mapper.Map<List<VoterDto>>(voters.Select(v => v.FirstRow)),
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

                    var teamMembers = team
                        .Where(id => usersById != null && usersById.ContainsKey(id))
                        .Select(id => mapper.Map<UserDto>(usersById![id]))
                        .ToList();

                    result.Results.Add(
                        new OptionResultDto
                        {
                            Id = i + 1,
                            DisplayText = string.Join(" + ", teamMembers.Select(m => m.Username)),
                            TeamMembers = teamMembers,
                            VoteCount = teamVotes.Count,
                            Percentage = Percent(teamVotes.Count, result.TotalVotes),
                            Voters = mapper.Map<List<VoterDto>>(teamVotes),
                        }
                    );
                }
                result.Results = result.Results.OrderByDescending(r => r.VoteCount).ToList();
                break;
        }

        // Free-text answers: every OpenText answer, plus any CustomPoll "Otro" answers
        // (rows carrying FreeText with no SelectedOptionId). Newest last (insertion order).
        result.FreeTextResponses = votes
            .Where(v => !string.IsNullOrWhiteSpace(v.FreeText))
            .Select(v => new FreeTextResponseDto
            {
                Username = v.User?.Username ?? "Unknown",
                Text = v.FreeText!,
            })
            .ToList();

        return result;
    }

    private static double Percent(int part, int total) =>
        total == 0 ? 0 : Math.Round((double)part / total * 100, 1);
}
