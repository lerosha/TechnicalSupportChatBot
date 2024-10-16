using Infrastructure.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnicalSupportBot.Controllers
{
    public class MappingController
    {
        private readonly IMappingRepository _mappingRepository;

        public MappingController(IMappingRepository mappingRepository)
        {
            _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
        }

        public async Task<int?> GetPreviousQuestionIdByAnswerIdAsync(int answerId)
        {
            return await _mappingRepository.GetPreviousQuestionIdByAnswerIdAsync(answerId);
        }

        public async Task<string> ProcessUserAnswerAsync(int previousQuestionId, int userAnswerId)
        {
            // Получаем следующий вопрос из репозитория
            var nextQuestionMapping = await _mappingRepository.GetNextQuestionMappingAsync(previousQuestionId, userAnswerId);

            if (nextQuestionMapping != null)
            {
                // Обработка следующего вопроса
                var nextQuestionText = nextQuestionMapping.NextQuestion.Text;
                return nextQuestionText;
            }
            else
            {
                return "Опрос завершен. Спасибо за участие!";
            }
        }
    }
}
