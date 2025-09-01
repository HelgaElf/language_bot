using LanguageBot.Interfaces;
using LanguageBot.Classes;
using LanguageBot.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageBot.Services
{
    public class UserService:  IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly user _user;

        public UserService(IUserRepository userRepository) 
         {
            _userRepository = userRepository;
            
         }

        public async Task RegisterUserAsync(user user, CancellationToken cancellationToken)
        {
             await _userRepository.RegisterUser(user);
        }

        public async Task<user> GetUserbyId(long userId)
        {
           return await _userRepository.GetUser(userId);
        }

        public async Task DeleteUserbyId(long userId)
        {
           await _userRepository.DeleteUser(userId);
        }
    }
}

