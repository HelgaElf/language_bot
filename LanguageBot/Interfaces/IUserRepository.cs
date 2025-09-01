using LanguageBot.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace LanguageBot.Interfaces
{
    public interface IUserRepository
    {
        public Task RegisterUser (user user);
        public Task<user> GetUser (long telegramUserID);
        public Task DeleteUser (long telegramUserID);
    }
}
