using Domain.DTO;
using EF.Repositories;
using Infrastructure.IRepositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace TechnicalSupportBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnswerController : ControllerBase
    {
        private readonly IAnswerRepository _answerRepository;
        private readonly IMappingRepository _mappingRepository;
        private readonly IUserRepository _userRepository;
        private readonly IQuestionRepository _questionRepository;

        public class NextQuestionInfo
        {
            public string Text { get; set; }
            public int? NextQuestionId { get; set; }
            public int? PreviousQuestionId { get; set; }
            public bool HasNextQuestion { get; set; }
        }

        public AnswerController(IAnswerRepository answerRepository, IMappingRepository mappingRepository, IUserRepository userRepository, IQuestionRepository questionRepository)
        {
            _answerRepository = answerRepository ?? throw new ArgumentNullException(nameof(answerRepository));
            _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _questionRepository = questionRepository ?? throw new ArgumentException(nameof(questionRepository));

        }

        public async Task<string> SendDocumentationFileAsync(int solutionId)
        {
            return await _answerRepository.GetDocumentationPathAsync(solutionId);
        }

        public async Task<int?> GetSolutionByTextAsync(string solutionText)
        {
            return await _mappingRepository.GetSolutionIdByTextAsync(solutionText);
        }

        public async Task<NextQuestionInfo> ProcessUserAnswerAsync(string userAnswer, long chatId)
        {
            var answerId = await _answerRepository.GetAnswerIdByTextAsync(userAnswer);
            if (answerId != null)
            {
                var previousQuestionId = await _mappingRepository.GetPreviousQuestionIdByAnswerIdAsync(answerId.Value);
                if (previousQuestionId.HasValue)
                {
                    var user = await _userRepository.GetUserByChatIdAsync(chatId);
                    if (user != null)
                    {
                        user.LastQuestionId = previousQuestionId.Value;
                        await _userRepository.UpdateUserAsync(user);
                    }
                    else
                    {
                        Console.WriteLine($"User with chat ID {chatId} not found.");
                    }

                    var nextQuestionMapping = await _mappingRepository.GetNextQuestionMappingAsync(previousQuestionId.Value, answerId);

                    if (nextQuestionMapping != null && nextQuestionMapping.NextQuestion != null)
                    {
                        var nextQuestionText = nextQuestionMapping.NextQuestion.Text;
                        var nextQuestionInfo = new NextQuestionInfo
                        {
                            Text = nextQuestionText,
                            NextQuestionId = nextQuestionMapping.NextQuestionId,
                            PreviousQuestionId = nextQuestionMapping.PreviousQuestionId,
                            HasNextQuestion = true
                        };
                        return nextQuestionInfo;
                    }
                    else
                    {
                        // Если следующего вопроса нет, возвращаем информацию о финальном решении
                        var finalSolutionText = await _mappingRepository.GetLastFinalAnswerAsync(previousQuestionId.Value, answerId.Value);
                        var finalSolutionInfo = new NextQuestionInfo
                        {
                            Text = finalSolutionText,
                            NextQuestionId = null,
                            PreviousQuestionId = nextQuestionMapping.PreviousQuestionId,
                            HasNextQuestion = false
                        };
                        return finalSolutionInfo;
                    }
                }
                else
                {
                    // Если ответ не найден, возвращаем объект с HasNextQuestion в false
                    var nextQuestionInfo = new NextQuestionInfo
                    {
                        Text = $"Answer with ID {answerId} not found.",
                        NextQuestionId = null,
                        PreviousQuestionId = null,
                        HasNextQuestion = false
                    };
                    return nextQuestionInfo;
                }
            }
            else
            {
                // Если ответ не найден, возвращаем информацию о предыдущем вопросе
                var user = await _userRepository.GetUserByChatIdAsync(chatId);
                var previousQuestionText = await _questionRepository.GetQuestionByIdAsync(user.LastQuestionId);
                var previousQuestionInfo = new NextQuestionInfo
                {
                    Text = $"Некорректный ответ. Пожалуйста, попробуйте еще раз.\n{previousQuestionText.Text}",
                    NextQuestionId = user.LastQuestionId, // Или можно вернуть предыдущий вопрос, чтобы не зацикливать пользователя на одном вопросе
                    PreviousQuestionId = user.LastQuestionId,
                    HasNextQuestion = true // Установите значение true, чтобы пользователь мог попробовать еще раз
                };
                return previousQuestionInfo;
            }
            
        }
    }
}
