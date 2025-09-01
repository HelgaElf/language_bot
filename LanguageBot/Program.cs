using LanguageBot.Classes;
using LanguageBot.Interfaces;
using LanguageBot.Repositories;
using LanguageBot.Services;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LanguageBot
{
    internal class Program
    {
        
        static async Task Main(string[] args)
        {
            Batteries_V2.Init();

            string dbPath = "D:/repos/github/LanguageBot/LanguageBot/language_bot.db";
            string connectionString = $"Data Source={dbPath}";

            IUserRepository userRepository = new UserRepository(connectionString);
            IUserService userService = new UserService(userRepository);
            IPhraseRepository phraseRepository = new PhraseRepository(connectionString);
            IPhraseService phraseService = new PhraseService(phraseRepository, connectionString);

            


            var commands = new List<BotCommand>
            {
                 new() { Command = "/start", Description = "Запустить бота" },
                 new() { Command = "/addphrase", Description = "Добавить новую фразу" },
                 new() { Command = "/practice", Description = "Тренировать изученные фразы" },
                 new() { Command = "/edit", Description = "Редактировать фразу" },
                 new() { Command = "/removephrase", Description = "Удалить фразу" },
                 new() { Command = "/info", Description = "Информация о боте" },
                 new() { Command = "/help", Description = "Помощь" },
                 
                
            };


            var handler = new UpdateHandler(userService, phraseService);


            string? token = Environment.GetEnvironmentVariable("LANGUAGE_BOT_TOKEN", EnvironmentVariableTarget.User);
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Bot Token is not found. Please, set TelegramBotToken environment variable.");
                return;
            }
            var botClient = new TelegramBotClient(token);
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message],
                DropPendingUpdates = true
            };


            var me = await botClient.GetMe();
            Console.WriteLine($"{me.FirstName} запущен!");


            var ct = new CancellationTokenSource();
            CancellationToken cancellationToken = ct.Token;

            await botClient.SetMyCommands(
                commands: commands,
                cancellationToken: cancellationToken
                );
            try
            {
                botClient.StartReceiving(handler, receiverOptions, cancellationToken);
                Console.WriteLine("Нажмите клавишу 'A' для выхода");
                while (!ct.IsCancellationRequested)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.A)
                    {
                        Console.WriteLine("\nЗавершение работы...");
                        ct.Cancel();
                        break;
                    }
                    else
                    {
                        var bot = await botClient.GetMe();
                        Console.WriteLine($"\nИнформация о боте:\n" +
                                        $"ID: {bot.Id}\n" +
                                        $"Username: @{bot.Username}\n" +
                                        $"Имя: {bot.FirstName}");
                    }
                }
                await Task.Delay(-1);
            }

            catch (Exception ex)
            {
                _ = handler.HandleErrorAsync(botClient, ex, Telegram.Bot.Polling.HandleErrorSource.HandleUpdateError, cancellationToken);

            }
          
        }
    }
}
