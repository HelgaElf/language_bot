using LanguageBot.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace LanguageBot.Interfaces
{
 
    public interface IPhraseRepository
    {
        public void Add(Phrase phrase);
        public void Remove(long phraseId, long userId);
        public Phrase Edit (Phrase phrase);
        public Phrase? Get(long phraseId);
        public List<Phrase> Find(long userId, string predicate);
        public List<Phrase> GetAllByUserId(long userId);
        public List<Phrase> GetAllOneWord (long userId);
        public List<Phrase> GetAllMultiWord (long userId);
        


    }
}
