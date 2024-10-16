using Domain.DTO;

namespace Infrastructure.IRepositories
{
    public interface IQuestionRepository
    {
        Task<Question> GetQuestionByIdAsync(int id);
        Task<Question> GetQuestionWithAnswersAsync(int questionId);
    }
}
