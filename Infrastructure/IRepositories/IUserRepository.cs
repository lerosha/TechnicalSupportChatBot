using Domain.DTO;


namespace Infrastructure.IRepositories
{
    public interface IUserRepository
    {
        Task<User> GetUserByChatIdAsync(long chatId);
        Task<User> GetUserByPhoneNumberAsync(string phoneNumber);
        Task UpdateUserAsync(User user);
        Task UpdateUserLastQuestionIdAsync(long chatId, int lastQuestionId);
        Task<bool> SetUserRatingAsync(long chatId, int rating);
    }
}
