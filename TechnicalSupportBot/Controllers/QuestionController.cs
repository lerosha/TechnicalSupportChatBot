using Domain.DTO;
using EF.Repositories;
using Infrastructure.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TechnicalSupportBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly IMappingRepository _mappingRepository;
        private readonly IAnswerRepository _answerRepository;

        public QuestionController(IQuestionRepository questionRepository, IMappingRepository mappingRepository, IAnswerRepository answerRepository)
        {
            _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));
            _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
            _answerRepository = answerRepository ?? throw new ArgumentNullException(nameof(answerRepository));
        }

        public async Task<ActionResult<Dictionary<string, int?>>> GetQuestionWithAnswersAsync(int questionId)
        {
            var question = await _questionRepository.GetQuestionWithAnswersAsync(questionId);

            if (question == null)
            {
                Console.WriteLine($"Question with ID {questionId} not found.");
                return NotFound();
            }

            var answers = question.Answers?.ToDictionary(answer => answer.Text, answer => answer.AnswerId);

            if (answers == null)
            {
                Console.WriteLine($"No answers found for question {questionId}.");
                // Если ответы пусты, вернуть пустой словарь
                return Ok(new Dictionary<string, int>());
            }

            Console.WriteLine($"Answers found for question {questionId}: {string.Join(", ", answers.Select(kv => $"{kv.Key}: {kv.Value}"))}");
            return Ok(answers);
        }

        public async Task<string> GetFirstQuestionTextAsync()
        {
            try
            {
                // Предположим, что первый вопрос имеет идентификатор 1
                var firstQuestion = await _questionRepository.GetQuestionByIdAsync(1);

                if (firstQuestion != null)
                {
                    return firstQuestion.Text;
                }
                else
                {
                    return "Первый вопрос не найден.";
                }
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Ошибка при получении первого вопроса: {ex.Message}");
                return "Произошла ошибка при получении первого вопроса.";
            }
        }

        public async Task<string> GetQuestionByIdAsync(int questionId)
        {
            var question = await _questionRepository.GetQuestionByIdAsync(questionId);

            if (question != null)
            {

                return question.Text;
            }
            else
            {
                return "No previous question";
            }
        }

    }
}
