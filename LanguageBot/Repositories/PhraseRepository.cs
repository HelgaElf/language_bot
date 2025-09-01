using LanguageBot.Classes;
using LanguageBot.Interfaces;
using LanguageBot.Services;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LanguageBot.Repositories
{
    public class PhraseRepository : IPhraseRepository
    {
        private readonly string _connectionString;

        public PhraseRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public void Add(Phrase phrase)
        {
            if (phrase == null)
                throw new ArgumentNullException(nameof(phrase));
            if (string.IsNullOrWhiteSpace(phrase.EnglishText))
                throw new ArgumentException("Фраза не может быть пустой");
            if (string.IsNullOrWhiteSpace(phrase.RussianText))
                throw new ArgumentException("Введите перевод фразы");

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var checkSql = "SELECT COUNT(1) FROM phrases WHERE user_id = @userId AND english = @english AND russian = @russian";
                using (var checkCommand = new SqliteCommand(checkSql, connection))
                {
                    checkCommand.Parameters.AddWithValue("@userId", phrase.UserId);
                    checkCommand.Parameters.AddWithValue("@english", phrase.EnglishText);
                    checkCommand.Parameters.AddWithValue("@russian", phrase.RussianText);

                    long exists = (long)checkCommand.ExecuteScalar();
                    if (exists > 0)
                        throw new InvalidOperationException("Вы уже добавляли эту фразу");
                }
                var isOneWord = IsOneWord(phrase.EnglishText);

                var insertSql = @"
    INSERT INTO phrases 
        (user_id, english, russian, is_one_word, created_at) 
    VALUES 
        (@userId, @englishText, @russianText, @isOneWord, @createdAt)";
                using (var insertCommand = new SqliteCommand(insertSql, connection))
                {

                    insertCommand.Parameters.AddWithValue("@userId", phrase.UserId);
                    insertCommand.Parameters.AddWithValue("@englishText", phrase.EnglishText);
                    insertCommand.Parameters.AddWithValue("@russianText", phrase.RussianText);
                    insertCommand.Parameters.AddWithValue("@isOneWord", isOneWord);
                    insertCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

                    insertCommand.ExecuteNonQuery();

                }

            }
        }
        public void Remove(long phraseId, long userId)
        {
            if (phraseId <= 0)
                throw new ArgumentException("Неверный Id фразы");
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var sql = "DELETE FROM phrases WHERE id = @id AND user_id = @userId";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", phraseId);
                    command.Parameters.AddWithValue("@userId", userId);
                    int affected = command.ExecuteNonQuery();

                    if (affected == 0)
                        throw new KeyNotFoundException("Фраза не найдена или не принадлежит пользователю");
                }
            }
        }
        public Phrase Edit(Phrase phrase)
        {
            if (phrase == null)
                throw new ArgumentNullException("Фраза не может быть пустой!");

            if (phrase.Id <= 0)
                throw new ArgumentException("У фразы должен быть ID");
            if (string.IsNullOrWhiteSpace(phrase.EnglishText))
                throw new ArgumentException("Английская фраза не может быть пустой");

            if (string.IsNullOrWhiteSpace(phrase.RussianText))
                throw new ArgumentException("Русская фраза не может быть пустой");

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var isOneWord = IsOneWord(phrase.EnglishText);
                var sql = @"
            UPDATE phrases 
            SET 
                english = @englishText,
                russian = @russianText,
                is_one_word = @isOneWord
            WHERE 
                id = @id AND user_id = @userId";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", phrase.Id);
                    command.Parameters.AddWithValue("@userId", phrase.UserId);
                    command.Parameters.AddWithValue("@englishText", phrase.EnglishText);
                    command.Parameters.AddWithValue("@russianText", phrase.RussianText);
                    command.Parameters.AddWithValue("@isOneWord", isOneWord);

                    int rowsUpdated = command.ExecuteNonQuery();

                    if (rowsUpdated == 0)
                        throw new Exception("Фраза не найдена или не принадлежит пользователю");
                }
            }

            return phrase;
        }
        public Phrase Get(long phraseId)
        {
            if (phraseId <= 0)
                throw new ArgumentException("Некорректный ID фразы", nameof(phraseId));
            
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var sql = @"
            SELECT 
                id, 
                user_id, 
                english, 
                russian, 
                is_one_word,
                created_at
            FROM phrases 
            WHERE id = @phraseId";
                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@phraseId", phraseId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Phrase
                            {
                                Id = reader.GetInt64(0),
                                UserId = reader.GetInt64(1),
                                EnglishText = reader.GetString(2),
                                RussianText = reader.GetString(3),
                                IsOneWord = Convert.ToBoolean(reader.GetInt32(4)),
                                CreatedAt = DateTime.Parse(reader.GetString(5))
                            };
                        }
                    }

                }
            }
            return null;
        }


        public List<Phrase> Find(long userId, string predicate)
        {
            if (string.IsNullOrWhiteSpace(predicate))
                throw new ArgumentException("Предикат поиска не может быть пустым");
            var foundPhrases = new List<Phrase>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var sql = @"
            SELECT id, user_id, english, russian, is_one_word, created_at
            FROM phrases
            WHERE user_id = @userId
            AND (english LIKE @predicate OR russian LIKE @predicate)";
                using (var command = new SqliteCommand(sql, connection))
                {
                    string searchPattern = $"%{predicate.Trim()}%";
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@predicate", searchPattern);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            foundPhrases.Add(new Phrase
                            {
                                Id = reader.GetInt64(0),
                                UserId = reader.GetInt64(1),
                                EnglishText = reader.GetString(2),
                                RussianText = reader.GetString(3),
                                IsOneWord = Convert.ToBoolean(reader.GetInt32(4)),
                                CreatedAt = DateTime.Parse(reader.GetString(5))
                            });
                        }
                    }
                }
            }

            return foundPhrases;
        }

        public List<Phrase> GetAllByUserId(long userId)
        {
            var phrases = new List<Phrase>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                    connection.Open();
                    var sql = "SELECT id, english, russian, is_one_word, created_at FROM phrases WHERE user_id = @userId";

                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                            var phrase = new Phrase
                            {
                                Id = reader.GetInt64(0),
                                EnglishText = reader.GetString(1),
                                RussianText = reader.GetString(2),
                                IsOneWord = reader.GetInt32(3) == 1,
                                CreatedAt = DateTime.Parse(reader.GetString(4)),
                                UserId = userId 
                            };
                            phrases.Add(phrase);
                            }
                        }
                    }
                    return phrases;
            }
        }
        public void UpdatePhrase(long phraseId, long userId, string englishText, string russianText)
        {
            if (phraseId <= 0) throw new ArgumentException("Invalid phrase ID");
            if (userId <= 0) throw new ArgumentException("Invalid user ID");
            if (string.IsNullOrWhiteSpace(englishText)) throw new ArgumentException("English text cannot be empty");
            if (string.IsNullOrWhiteSpace(russianText)) throw new ArgumentException("Russian text cannot be empty");

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var sql = @"
            UPDATE phrases 
            SET english = @englishText, 
                russian = @russianText
            WHERE id = @phraseId AND user_id = @userId";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@phraseId", phraseId);
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@englishText", englishText);
                    command.Parameters.AddWithValue("@russianText", russianText);
                    int affectedRows = command.ExecuteNonQuery();
                    if (affectedRows == 0)
                        throw new KeyNotFoundException("Phrase not found or not owned by user");
                }
            }
        }
        public List<Phrase> GetAllOneWord(long userId)
        {
            var phrases = new List<Phrase>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var sql = @"
            SELECT id, user_id, english, russian, created_at
            FROM phrases
            WHERE user_id = @userId AND is_one_word = 1";
                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            phrases.Add(new Phrase
                            {
                                Id = reader.GetInt64(0),
                                UserId = reader.GetInt64(1),
                                EnglishText = reader.GetString(2),
                                RussianText = reader.GetString(3),
                                IsOneWord = true,
                                CreatedAt = DateTime.Parse(reader.GetString(4))
                            });
                        }
                    }
                }
            }

            return phrases;
        }
        public List<Phrase> GetAllMultiWord(long userId)
        {
            var phrases = new List<Phrase>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var sql = @"
            SELECT id, user_id, english, russian, created_at
            FROM phrases
            WHERE user_id = @userId AND is_one_word = 0";
                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            phrases.Add(new Phrase
                            {
                                Id = reader.GetInt64(0),
                                UserId = reader.GetInt64(1),
                                EnglishText = reader.GetString(2),
                                RussianText = reader.GetString(3),
                                IsOneWord = false,
                                CreatedAt = DateTime.Parse(reader.GetString(4))
                            });
                        }
                    }
                }

            }
            return phrases;
        }
        public int IsOneWord(string input)
        {
            var wordCount = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return wordCount == 1 ? 1 : 0;
        }
         
    }
}
