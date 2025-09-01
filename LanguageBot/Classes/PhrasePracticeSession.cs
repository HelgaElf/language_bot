using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageBot.Classes
{
    internal class PhrasePracticeSession
    {
            public required Phrase CurrentPhrase { get; set; }
            public int AttemptsLeft { get; set; }
            public string? CorrectAnswer { get; set; }
            public bool IsTranslatingToRussian { get; set; }
            public string? HiddenPart { get; set; }
            }
}
