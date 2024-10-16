using Domain.DTO;
using Microsoft.EntityFrameworkCore;
using Infrastructure.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EF.Repositories
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly ProjectContext _contextDB;

        public QuestionRepository(ProjectContext contextDB)
        {
            _contextDB = contextDB ?? throw new ArgumentException(nameof(DbContext));
        }

        public async Task<Question> GetQuestionWithAnswersAsync(int questionId)
        {
            return await _contextDB.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);
        }

        public async Task<Question> GetQuestionByIdAsync(int questionId)
        {
            return await _contextDB.Questions.FindAsync(questionId);
        }

        
        public async Task<Question> GetFirstQuestionAsync()
        {
            try
            {
                
                var firstQuestion = await _contextDB.Questions.FirstOrDefaultAsync();
                return firstQuestion;
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error getting the first question: {ex.Message}");
                return null;
            }
        }

        public async Task<Question> GetNextQuestionAsync(int previousQuestionId, int userAnswerId)
        {
            try
            {
                // Находим маппинг вопроса и ответа пользователя
                var questionMapping = await _contextDB.QuestionAnswerMappings
                    .FirstOrDefaultAsync(mapping =>
                        mapping.PreviousQuestionId == previousQuestionId &&
                        mapping.AnswerId == userAnswerId);

                
                var nextQuestion = questionMapping?.NextQuestion;

                return nextQuestion;
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error getting the next question: {ex.Message}");
                return null;
            }
        }
    }
}
