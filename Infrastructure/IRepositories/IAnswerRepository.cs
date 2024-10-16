using Domain.DTO;

namespace Infrastructure.IRepositories
{
    public interface IAnswerRepository
    {
        Task<int?> GetAnswerIdByTextAsync(string answerId);
        Task<List<Answer>> GetAnswersByQuestionIdAsync(int questionId);
        Task<string> GetDocumentationPathAsync(int solutionId);
    }
}
