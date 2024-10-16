using Domain.DTO;

namespace Infrastructure.IRepositories
{
    public interface IMappingRepository
    {
        Task<QuestionAnswerMapping> GetNextQuestionMappingAsync(int previousQuestionId, int? userAnswerId);
        Task<int?> GetPreviousQuestionIdByAnswerIdAsync(int answerId);
        Task<string> GetLastFinalAnswerAsync(int solutionId, int answerId);
        Task<int?> GetSolutionIdByTextAsync(string solutionText);
    }
}
