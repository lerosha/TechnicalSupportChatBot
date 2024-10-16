using Domain.DTO;
using Infrastructure.IRepositories;
using Telegram.Bot.Types;

namespace TechnicalSupportBot.Controllers
{
    public class UserController
    {
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<bool> IsUserAuthorizedAsync(long chatId)
        {
            // Проверить, существует ли пользователь с указанным номером телефона в базе данных
            var user = await _userRepository.GetUserByChatIdAsync(chatId);
            return user != null && user.IsAuthorized;
        }

        public async Task<bool> SetUserRatingAsync(long chatId, int raiting)
        {
            var user = await _userRepository.SetUserRatingAsync(chatId, raiting);
            return user != null && user;
        }

        public async Task<int> GetPreviousQuestionId(long chatId)
        {
            var user = await _userRepository.GetUserByChatIdAsync(chatId);
            return user.LastQuestionId;
        }
        public async Task UpdateLastQuestionId(long chatId, int question)
        {
            var user = await _userRepository.GetUserByChatIdAsync(chatId);
            if (user != null && user.ChatId == chatId)
            {
                user.LastQuestionId = question;
                await _userRepository.UpdateUserAsync(user);
            }

        }

        public async Task<bool> AuthorizeUserAsync(long chatId, string phoneNumber)
        {
            try
            {
                
                var user = await _userRepository.GetUserByPhoneNumberAsync(phoneNumber);              

                if (user != null && user.ChatId == chatId)
                {
                    user.ChatId = chatId;
                    user.IsAuthorized = true;
                    await _userRepository.UpdateUserAsync(user);

                    return true; 
                }
                else
                {
                    return false; 
                }
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error authorizing user: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateUserLastQuestionIdAsync(long chatId, int lastQuestionId)
        {
            await _userRepository.UpdateUserLastQuestionIdAsync(chatId, lastQuestionId);
        }
    }
}
