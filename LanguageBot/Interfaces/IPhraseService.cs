using LanguageBot.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LanguageBot.Interfaces
{
    public interface IPhraseService
    {
        void AddPhrase(long userId, string englishText, string russianText);
        void RemovePhrase(long phraseId, long userId);
        Phrase GetPhrase(long phraseId);
        //List <Phrase> FindPhrase(long userId, string searchTerm);
        List<Phrase> GetAllByUserId(long userId);
        List<Phrase> GetAllOneWordPhrases(long userId);
        List<Phrase> GetAllMultiWordPhrases(long userId);
        public Task UpdatePhrase(long userId, long phraseId, string newEnglish, string newRussian);
    }
}
