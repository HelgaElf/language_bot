using LanguageBot.Classes;
using LanguageBot.Interfaces;
using LanguageBot.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LanguageBot.Services
{
    public class PhraseService : IPhraseService
    {
        private readonly IPhraseRepository _phraseRepository;
        private readonly string _connectionString;

        public PhraseService(IPhraseRepository phraseRepository, string connectionString)
        {
            _phraseRepository = phraseRepository ?? throw new ArgumentNullException(nameof(phraseRepository));
            _connectionString = connectionString;
        }
        public void AddPhrase(long userId, string englishText, string russianText) 
        {
            if (userId <= 0)
                throw new ArgumentException("Некорректный ID пользователя");

            if (string.IsNullOrWhiteSpace(englishText))
                throw new ArgumentException("Английская фраза не может быть пустой");

            if (string.IsNullOrWhiteSpace(russianText))
                throw new ArgumentException("Русский перевод не может быть пустым");

            string trimmedEnglish = englishText.Trim();
            string trimmedRussian = russianText.Trim();

            string normalizedEnglish = trimmedEnglish.ToLowerInvariant();
            string normalizedRussian = trimmedRussian.ToLowerInvariant();
            bool isOneWord = normalizedEnglish.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length == 1;

            var userPhrases = _phraseRepository.GetAllByUserId(userId);
            bool alreadyExists = userPhrases.Any(p =>
              p.EnglishText.Equals(normalizedEnglish, StringComparison.OrdinalIgnoreCase) &&
              p.RussianText.Equals(normalizedRussian, StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
                throw new ApplicationException($"Фраза \"{normalizedEnglish} - {normalizedRussian}\" уже существует.");


            var phrase = new Phrase
            {
                UserId = userId,
                EnglishText = normalizedEnglish,
                RussianText = normalizedRussian,
                IsOneWord = isOneWord,
                CreatedAt = DateTime.UtcNow,
            };
            
            try
            {
                _phraseRepository.Add(phrase);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Не удалось сохранить фразу: {englishText}", ex);
            }
        }

        public void RemovePhrase(long phraseId, long userId)
        {
            if (phraseId <= 0) throw new ArgumentException("Invalid phrase ID");
            if (userId <= 0) throw new ArgumentException("Invalid user ID");

            try
            {
                _phraseRepository.Remove(phraseId, userId);
            }
            catch (KeyNotFoundException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to delete phrase", ex);
            }

        }

        public Phrase GetPhrase(long phraseId)
        {
            return _phraseRepository.Get(phraseId)
                ?? throw new KeyNotFoundException("Phrase not found");
        }

        public List<Phrase> GetAllByUserId(long userId)
        {
            return _phraseRepository.GetAllByUserId(userId);
        }

        public List<Phrase> GetAllOneWordPhrases(long userId)
        {
            return _phraseRepository.GetAllByUserId(userId);
        }

        public List<Phrase> GetAllMultiWordPhrases(long userId)
        {
            return _phraseRepository.GetAllByUserId(userId);
        }

        public async Task UpdatePhrase(long userId, long phraseId, string newEnglish, string newRussian)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var sql = "UPDATE phrases SET english=@english, russian=@russian, is_one_word=@isOneWord WHERE id=@id AND user_id=@userId";
                bool isOneWord = newEnglish.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length == 1;
                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@english", newEnglish);
                    command.Parameters.AddWithValue("@russian", newRussian);
                    command.Parameters.AddWithValue("@isOneWord", isOneWord ? 1 : 0);
                    command.Parameters.AddWithValue("@id", phraseId);
                    command.Parameters.AddWithValue("@userId", userId);

                    command.ExecuteNonQuery();
                }
            }
        }

      
    }
}
