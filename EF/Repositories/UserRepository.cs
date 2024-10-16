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
    public class UserRepository : IUserRepository
    {
        private readonly ProjectContext _contextDB;

        public UserRepository(ProjectContext contextDB)
        {
            _contextDB = contextDB ?? throw new ArgumentException(nameof(DbContext));
        }


        public async Task<User> GetUserByChatIdAsync(long chatId)
        {
            return await _contextDB.Users.FirstOrDefaultAsync(u => u.ChatId == chatId);
        }

        public async Task<User> GetUserByPhoneNumberAsync(string phoneNumber)
        {
            return await _contextDB.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        }

        public async Task UpdateUserAsync(User user)
        {
            _contextDB.Users.Update(user);
            await _contextDB.SaveChangesAsync();
        }

        public async Task UpdateUserLastQuestionIdAsync(long chatId, int lastQuestionId)
        {
            var user = await _contextDB.Users.FirstOrDefaultAsync(u => u.ChatId == chatId);
            if (user != null)
            {
                user.LastQuestionId = lastQuestionId;
                await _contextDB.SaveChangesAsync();
            }
        }

        public async Task<bool> SetUserRatingAsync(long chatId, int rating)
        {
            var user = await GetUserByChatIdAsync(chatId);
            if (user != null)
            {
                user.Rating = rating;
                await UpdateUserAsync(user);
                return true;
            }
            return false;
        }
    }
}
