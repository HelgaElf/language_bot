using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageBot.Classes
{
   public class UserState
    {
        public string CurrentCommand { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }
}
