using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageBot.Classes
{
    public class user
    {
            public long TelegramUserId
            { get; set; }
            public string? TelegramUserName
            { get; set; }
            public DateTime RegisteredAt
            { get; set; }

    }
}
