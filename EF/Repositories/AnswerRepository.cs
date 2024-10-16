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
    public class AnswerRepository : IAnswerRepository
    {
        private readonly ProjectContext _contextDB;

        public AnswerRepository(ProjectContext dbContext)
        {
            _contextDB = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<Answer>> GetAnswersByQuestionIdAsync(int questionId)
        {
            return await _contextDB.Answers
                .Where(a => a.QuestionId == questionId)
                .ToListAsync();
        }

        public async Task<int?> GetAnswerIdByTextAsync(string answerText)
        {
            try
            {
                var answer = await _contextDB.Answers
                    .Where(a => a.Text == answerText)
                    .FirstOrDefaultAsync();

                return answer?.AnswerId;
            }
            catch (NullReferenceException)
            {
                
                return null;
            }
        }

        public async Task<string> GetDocumentationPathAsync(int solutionId)
        {
            var solution = await _contextDB.Solutions.FirstOrDefaultAsync(s => s.SolutionId == solutionId);
            if (solution != null)
            {
                return solution?.DocPath;
            }
            else
            {
                return null;
            }
        }
    }
}
