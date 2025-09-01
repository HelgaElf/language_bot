using LanguageBot.Classes;
using LanguageBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace LanguageBot.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task RegisterUser(user user)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var checkSql = "SELECT COUNT(1) FROM users WHERE telegram_user_id = @Id";
                using (var checkCommand = new SqliteCommand(checkSql, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Id", user.TelegramUserId);
                    long exists = Convert.ToInt64(checkCommand.ExecuteScalar());

                    if (exists > 0)
                    {
                        throw new Exception("Пользователь уже зарегистрирован!");
                    }
                }

                var insertSql = @"
            INSERT INTO users (telegram_user_id, user_name, registered_at) 
            VALUES (@telegram_user_id,@user_name, @registered_at)";

                using (var insertCommand = new SqliteCommand(insertSql, connection))
                {
                    insertCommand.Parameters.AddWithValue("@telegram_user_id", user.TelegramUserId);
                    insertCommand.Parameters.AddWithValue("@user_name", user.TelegramUserName);
                    insertCommand.Parameters.AddWithValue("@registered_at", DateTime.UtcNow);

                    insertCommand.ExecuteNonQuery();
                }
            }
        }

        public  async Task<user> GetUser(long telegramUserID)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var sql = @"
            SELECT 
                telegram_user_id,
                user_name,
                registered_at
            FROM users
            WHERE telegram_user_id = @telegramId";
                using (var command = new SqliteCommand(sql, connection))
                {
                    
                    command.Parameters.AddWithValue("@telegramId", telegramUserID);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read()) 
                        {
                            return new user
                            {
                                TelegramUserId = reader.GetInt64(0),
                                TelegramUserName = reader.IsDBNull(1)                 
                            ? null
                            : reader.GetString(2),
                                RegisteredAt = DateTime.Parse(reader.GetString(2)) 
                            };
                        }
                    }
                }
            }

            return null;
        }
        public async Task DeleteUser(long telegramUserID)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var sql = "DELETE FROM users WHERE telegram_user_id = @userId";
                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@userId", telegramUserID);
                    command.ExecuteNonQuery();

                }

            }

        }
    }
}
