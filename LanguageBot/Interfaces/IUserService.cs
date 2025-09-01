using LanguageBot.Classes;
using LanguageBot.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageBot.Interfaces
{
    public interface IUserService
    {
       public Task RegisterUserAsync(user user, CancellationToken ct);

       public Task<user> GetUserbyId (long userId);

        public Task DeleteUserbyId(long userId);
    }

    
}
