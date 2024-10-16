using Telegram.Bot;
using Telegram.Bot.Args;
using System;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using TechnicalSupportBot.Controllers;
using System.Threading;
using Domain.DTO;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Passport;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace TechnicalSupportBot
{
    public class TelegramBot
    {
        private readonly TelegramBotClient _botClient;
        private readonly QuestionController _questionController;
        private readonly AnswerController _answerController;
        private readonly UserController _userController;
        private readonly long _supportAgentChatId = 294482224; // Chat ID агента поддержки
        private readonly Dictionary<long, long> _supportSessions = new(); // Сессии поддержки (пользователь -> агент)
        private readonly List<long> _supportQueue = new(); // Очередь ожидания поддержки


        public TelegramBot(string accessToken, QuestionController questionController, AnswerController answerController, UserController userController)
        {
            _botClient = new TelegramBotClient(accessToken);
            _questionController = questionController ?? throw new ArgumentNullException(nameof(questionController));
            _answerController = answerController ?? throw new ArgumentNullException(nameof(answerController));
            _userController = userController ?? throw new ArgumentNullException(nameof(userController));
            

        }

        public async Task ListenForMessagesAsync()
        {
            using CancellationTokenSource cts = new();

            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Start listening for @{me.Username}");   
            Console.ReadLine();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {

            if (update.CallbackQuery != null)
            {
                await HandleCallbackQueryAsync(update.CallbackQuery, cancellationToken);
                return;
            }

            if (update.Message == null || update.Message.Text == null)
                return;

            var message = update.Message;
            var chatId = message.Chat.Id;

            if (chatId == _supportAgentChatId)
            {
                await HandleAgentSupportAsync(message, cancellationToken);
                return;
            }

            if (_supportSessions.ContainsKey(chatId))
            {
                var agentChatId = _supportSessions[chatId];
                await _botClient.SendTextMessageAsync(agentChatId, message.Text, cancellationToken: cancellationToken);
            }
            else
            {
                await ProcessUserInput(chatId, message.Text, cancellationToken, update);
            }

        }

        private async Task HandleAgentSupportAsync(Message message, CancellationToken cancellationToken)
        {
            var messageText = message.Text;
            var agentChatId = message.Chat.Id;

            if (messageText.StartsWith("/connect"))
            {
                var parts = messageText.Split(' ');
                if (parts.Length == 2 && long.TryParse(parts[1], out long userChatId))
                {
                    await ConnectSupportAgentAsync(agentChatId, userChatId);
                }
            }
            else if (messageText.Equals("/end_support", StringComparison.OrdinalIgnoreCase))
            {
                var userChatId = _supportSessions.FirstOrDefault(x => x.Value == agentChatId).Key;
                await EndSupportSessionAsync(userChatId);
            }
            else
            {
                var userChatId = _supportSessions.FirstOrDefault(x => x.Value == agentChatId).Key;
                if (userChatId != 0)
                {
                    await _botClient.SendTextMessageAsync(userChatId, messageText, cancellationToken: cancellationToken);
                }
            }
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            // Обработка CallbackQuery
            if (callbackQuery.Data.StartsWith("rating_"))
            {
                int rating = int.Parse(callbackQuery.Data.Split('_')[1]);

                bool isRatingSet = await _userController.SetUserRatingAsync(callbackQuery.Message.Chat.Id, rating);

                if (isRatingSet)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: "Спасибо за вашу оценку!",
                        cancellationToken: cancellationToken
                    );

                    
                    await _botClient.EditMessageReplyMarkupAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        messageId: callbackQuery.Message.MessageId,
                        replyMarkup: null,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: "Не удалось сохранить вашу оценку. Пожалуйста, попробуйте еще раз.",
                        cancellationToken: cancellationToken
                    );
                }

                return;
            }

            if (callbackQuery.Data == "restart_poll")
            {
                // Отправка первого вопроса
                var firstQuestionText = await _questionController.GetFirstQuestionTextAsync();
                await _botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: firstQuestionText,
                    cancellationToken: cancellationToken
                );
                await SendQuestionWithAnswersAsync(callbackQuery.Message.Chat.Id, 1, cancellationToken);
            }
            else if (callbackQuery.Data.StartsWith("back_"))
            {
                int lastQuestionId = int.Parse(callbackQuery.Data.Split('_')[1]);
                // Отправка предыдущего вопроса
                await SendPreviousQuestionAsync(callbackQuery.Message.Chat.Id, cancellationToken, lastQuestionId);

                // Скрыть клавиатуру после отправки предыдущего вопроса
                await _botClient.EditMessageReplyMarkupAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    replyMarkup: null,
                    cancellationToken: cancellationToken
                );
            }
        }

        private async Task SendDocumentationFileAsync(long chatId, int solutionId, CancellationToken cancellationToken)
        {
                        
            var filePath = await _answerController.SendDocumentationFileAsync(solutionId);

            if (string.IsNullOrEmpty(filePath))
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Документация не найдена.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            try
            {
                
                await _botClient.SendDocumentAsync(
                    chatId: chatId,
                    document: InputFile.FromUri(filePath),
                    parseMode: ParseMode.Html,
                    caption: "Вот нужная Вам документация.",
                    cancellationToken: cancellationToken
                );

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending document: {ex.Message}");
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка при отправке документации.",
                    cancellationToken: cancellationToken
                );
            }
        }

        public async Task ConnectSupportAgentAsync(long agentChatId, long userChatId)
        {
            if (_supportQueue.Contains(userChatId))
            {
                _supportQueue.Remove(userChatId);
                _supportSessions[userChatId] = agentChatId;

                await _botClient.SendTextMessageAsync(userChatId, "Вы подключены к агенту поддержки.", cancellationToken: CancellationToken.None);
                await _botClient.SendTextMessageAsync(agentChatId, $"Вы подключены к пользователю {userChatId}.", cancellationToken: CancellationToken.None);
            }
        }

        public async Task EndSupportSessionAsync(long userChatId)
        {
            if (_supportSessions.ContainsKey(userChatId))
            {
                var agentChatId = _supportSessions[userChatId];
                _supportSessions.Remove(userChatId);

                await _botClient.SendTextMessageAsync(userChatId, "Сессия поддержки завершена. Вы можете продолжить использование бота.", cancellationToken: CancellationToken.None);
                await _botClient.SendTextMessageAsync(agentChatId, $"Сессия поддержки с пользователем {userChatId} завершена.", cancellationToken: CancellationToken.None);

                await SendRatingPollAsync(userChatId);
                await SendRestartPollButtonAsync(userChatId);
            }
        }

        private async Task ProcessUserInput(long chatId, string userInput, CancellationToken cancellationToken, Update update)
        {
            try
            {
                try
                {
                    var isAuthorized = await _userController.IsUserAuthorizedAsync(chatId);

                    if (!isAuthorized)
                    {
                        var phoneNumber = await WaitForUserResponseAsync(chatId, cancellationToken, update);

                        if (!string.IsNullOrEmpty(phoneNumber))
                        {
                            var authorizationStatus = await _userController.AuthorizeUserAsync(chatId, phoneNumber);

                            if (authorizationStatus)
                            {
                                await _botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "Авторизация успешная. Вы можете начинать использование бота.",
                                    cancellationToken: cancellationToken
                                );

                                var firstQuestionText = await _questionController.GetFirstQuestionTextAsync();
                                await _botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: firstQuestionText,
                                    cancellationToken: cancellationToken
                                );
                                await SendQuestionWithAnswersAsync(chatId, 1, cancellationToken);
                            }
                            else
                            {
                                await _botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "Authorization failed. Please try again.",
                                    cancellationToken: cancellationToken
                                );
                            }
                        }
                    }
                    else
                    {
                        if (userInput.Equals("Другое", StringComparison.OrdinalIgnoreCase))
                        {
                            await _botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "С Вашей проблемой необходимо обратиться на линию поддержки. Пожалуйста, ожидайте подключения агента.",
                                cancellationToken: cancellationToken
                            );

                            _supportQueue.Add(chatId);
                            Console.WriteLine($"Запрос на поддержку от пользователя {chatId}");
                            Console.WriteLine("Запрос на поддержку от пользователя 294482224");
                            Console.WriteLine("Запрос на поддержку от пользователя 257147811");
                            Console.WriteLine("Запрос на поддержку от пользователя 684381235");
                            Console.WriteLine("Запрос на поддержку от пользователя 513845685");
                            Console.WriteLine("Запрос на поддержку от пользователя 748532459");
                            Console.WriteLine("Запрос на поддержку от пользователя 474637485");
                            await _botClient.SendTextMessageAsync(_supportAgentChatId, $"Новый запрос на поддержку от пользователя {chatId}.", cancellationToken: cancellationToken);

                            return;
                        }

                        var response = await _answerController.ProcessUserAnswerAsync(userInput, chatId);

                        if (response.HasNextQuestion)
                        {
                            var inlineKeyboard = new InlineKeyboardMarkup(new[]
                            {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Назад", $"back_{response.PreviousQuestionId}")
                                }
                            });

                            await _botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: response.Text,
                                replyMarkup: inlineKeyboard,
                                cancellationToken: cancellationToken
                            );

                            await _userController.UpdateLastQuestionId(chatId, response.NextQuestionId.Value);
                            await SendQuestionWithAnswersAsync(chatId, response.NextQuestionId.Value, cancellationToken);
                        }
                        else
                        {
                            var solutionId = await _answerController.GetSolutionByTextAsync(response.Text);
                            var doc = await _answerController.SendDocumentationFileAsync(solutionId.Value);
                            if (doc != null)
                            {
                                await _botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: response.Text,
                                cancellationToken: cancellationToken
                                );
                                await SendDocumentationFileAsync(chatId, solutionId.Value, cancellationToken);
                                await SendRatingPollAsync(chatId);
                                await SendRestartPollButtonAsync(chatId);
                            }
                            else
                            {
                                await _botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: response.Text,
                                cancellationToken: cancellationToken
                                );
                                await SendRatingPollAsync(chatId);
                                await SendRestartPollButtonAsync(chatId);
                            }
                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing user input: {ex.Message}");
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "An error occurred while processing your input. Please try again later.",
                        cancellationToken: cancellationToken
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing user input: {ex.Message}");
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "An error occurred while processing your input. Please try again later.",
                    cancellationToken: cancellationToken
                );
            }
        }

        private async Task SendPreviousQuestionAsync(long chatId, CancellationToken cancellationToken, int LastQuestionId)
        {
            
                        
            var previousQuestionText = _questionController.GetQuestionByIdAsync(LastQuestionId);

            await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: previousQuestionText.Result,
                    cancellationToken: cancellationToken
                );

            
            await SendQuestionWithAnswersAsync(chatId, LastQuestionId, cancellationToken);
        }

        private async Task<string> WaitForUserResponseAsync(long chatId, CancellationToken cancellationToken, Update update)
        {
            string phoneNumber = update.Message.Text;
            if (!string.IsNullOrEmpty(phoneNumber) && phoneNumber.StartsWith("89"))
            {
                return phoneNumber;
            }
            else
            {
                return null; 
            }
        }

        public async Task SendRatingPollAsync(long chatId)
        {
            // Создание кнопок для оценок от 1 до 5
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("1", "rating_1"),
                    InlineKeyboardButton.WithCallbackData("2", "rating_2"),
                    InlineKeyboardButton.WithCallbackData("3", "rating_3"),
                    InlineKeyboardButton.WithCallbackData("4", "rating_4"),
                    InlineKeyboardButton.WithCallbackData("5", "rating_5")
                }
            });

            
            var messageText = "Пожалуйста, оцените качество поддержки от 1 до 5";

            // Отправка сообщения с клавиатурой пользователю
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: messageText,
                replyMarkup: inlineKeyboard
            );


        }

        public async Task SendRestartPollButtonAsync(long chatId)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("Начать опрос заново", "restart_poll")
                }
            });

            var messageText = "Вы успешно завершили прохождение опроса. Хотите начать заново?";

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: messageText,
                replyMarkup: inlineKeyboard
            );
        }

        private async Task SendQuestionWithAnswersAsync(long chatId, int questionId, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _questionController.GetQuestionWithAnswersAsync(questionId);

                // Проверяем тип ответа
                if (response.Result is OkObjectResult result && result.Value is Dictionary<string, int> answers)
                {
                    // Если ответ успешен и содержит ответы, создаем кнопки для ответов
                    var buttons = new List<KeyboardButton[]>();

                    foreach (var answer in answers)
                    {
                        buttons.Add(new KeyboardButton[] { new KeyboardButton(answer.Key) });
                    }
                    
                    
                    buttons.Add(new KeyboardButton[] { new KeyboardButton("Другое") });

                    var replyMarkup = new ReplyKeyboardMarkup(buttons.ToArray());
                    replyMarkup.ResizeKeyboard = true;
                    replyMarkup.OneTimeKeyboard = true;
                    replyMarkup.Selective = true;

                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Выберите ответ.",
                        replyMarkup: replyMarkup,
                        cancellationToken: cancellationToken
                    );

                    
                }
                else if (response.Result is NotFoundResult)
                {
                    
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вопрос не найден.",
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Произошла ошибка при отправке вопроса с ответами.",
                        cancellationToken: cancellationToken
                    );
                }
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error sending question with answers: {ex.Message}");
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка при отправке вопроса с ответами.",
                    cancellationToken: cancellationToken
                );
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}
