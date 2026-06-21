// Services/IQuestionsService.cs

namespace VayaPreguntita.API.Services;

using VayaPreguntita.API.DTOs.Questions;

public interface IQuestionsService
{
    Task<IEnumerable<QuestionDto>> GetPoolAsync(int groupId);
    Task<QuestionResultDto?> GetByDateAsync(int groupId, DateOnly date);
    Task<QuestionDto> CreateAsync(int groupId, int creatorId, CreateQuestionDto dto);
}
