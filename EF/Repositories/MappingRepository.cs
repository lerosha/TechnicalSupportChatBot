using Domain.DTO;
using Infrastructure.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EF.Repositories
{
    public class MappingRepository : IMappingRepository
    {
        private readonly ProjectContext _contextDB;

        public MappingRepository(ProjectContext dbContext)
        {
            _contextDB = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
        public async Task<string> GetLastFinalAnswerAsync(int prevQuestionId, int answerId)
        {
            // Находим последнюю запись QuestionAnswerMapping для данного решения
            var lastMapping = await _contextDB.QuestionAnswerMappings
                .Where(qam => qam.PreviousQuestionId == prevQuestionId && qam.AnswerId == answerId)
                .OrderByDescending(qam => qam.MappingId)
                .FirstOrDefaultAsync();

            if (lastMapping != null)
            {
                // Получаем объект Solution по его идентификатору
                var solution = await _contextDB.Solutions.FindAsync(lastMapping.SolutionId);

                if (solution != null)
                {
                    
                    return solution.Text;
                }
                else
                {
                    
                    return "No text for solution";
                }
            }
            else
            {               
                return "No last final answer found.";
            }
        }

        public async Task<QuestionAnswerMapping> GetNextQuestionMappingAsync(int previousQuestionId, int? userAnswerId)
        {
            var mapping = await _contextDB.QuestionAnswerMappings
        .Include(qam => qam.NextQuestion)
        .FirstOrDefaultAsync(qam => qam.PreviousQuestionId == previousQuestionId && qam.AnswerId == userAnswerId);

            return mapping;
        }
        public async Task<int?> GetSolutionIdByTextAsync(string solutionText)
        {
            var solutionId = await _contextDB.Solutions
                .Where(a => a.Text == solutionText)
                    .FirstOrDefaultAsync();

            return solutionId?.SolutionId;
        }

        public async Task<int?> GetPreviousQuestionIdByAnswerIdAsync(int answerId)
        {
            var mapping = await _contextDB.QuestionAnswerMappings.FirstOrDefaultAsync(qam => qam.AnswerId == answerId);
            return mapping?.PreviousQuestionId;
        }
    }
}
