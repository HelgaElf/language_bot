using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageBot.Classes
{
    public class Phrase
    {
        public long Id { get; set; }
        public string? EnglishText { get; set; } 
        public string? RussianText { get; set; }
        public long UserId { get; set; }
        public bool IsOneWord { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
