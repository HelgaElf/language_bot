using LanguageBot.Interfaces;
using LanguageBot.Repositories;
using LanguageBot.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Collections.Specialized.BitVector32;



namespace LanguageBot.Classes
{
    public class UpdateHandler : IUpdateHandler
    {

        private readonly IPhraseService _phraseService;
        private readonly IUserService _userService;
        private readonly Dictionary<long, UserState> _userStates = new();
        private static readonly Dictionary<long, PhrasePracticeSession> _practiceSessions = new();


        public UpdateHandler(IUserService userService, IPhraseService phraseService)
        {
            _userService = userService;
            _phraseService = phraseService;
            
        }


        string? currentCommand = null;
        bool waitText = false;
        private const string ExitPracticeCommand = "/stop";
        private const string ExitEditCommand = "/stop";
        private bool _isEditingPhrase = false;
        private long _editPhraseId = 0;

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {

            string? input = update.Message.Text;
            long userID = update.Message.From.Id;
            var user = await _userService.GetUserbyId(userID);
           

        string? command = input?.ToLower().Split(' ')[0];

            if (command == ExitPracticeCommand)
            {
                waitText = false;
                currentCommand = null;
                _practiceSessions.Remove(userID);

                await botClient.SendMessage(
                    update.Message.Chat,
                    text: "Команда завершена.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            if (_isEditingPhrase)
            {
                await SaveEditedPhrase(botClient, update, cancellationToken);
                return;
            }

            if (waitText && currentCommand == "/practice")
            {
                if (_practiceSessions.TryGetValue(userID, out var session))
                {
                    await HandlePracticeResponse(botClient, update, session, cancellationToken);
                }
                return; 
            }

            if (user == null && command != "/help" && command != "/info" && command != "/start")
            {
                _ = await botClient.SendMessage(update.Message.Chat,
               text: "Для начала работы нажмите СТАРТ",
               cancellationToken: cancellationToken);

            }

            if (_isEditingPhrase && update.Message?.Text != null)
            {
                
                if (string.Equals(update.Message.Text.Trim(), ExitEditCommand, StringComparison.OrdinalIgnoreCase))
                {
                    _isEditingPhrase = false;
                    _editPhraseId = 0;
                    await botClient.SendMessage(update.Message.Chat, "Редактирование отменено.");
                    return;
                }

                await SaveEditedPhrase(botClient, update, cancellationToken);
                return; 
            }

            if (currentCommand != null)
                command = currentCommand;
            switch (command)
            {
                case "/start":
                    await Start(botClient, update, cancellationToken); 
                    break;
                case "/help": 
                    await Help(botClient, update, cancellationToken);
                    break;
                case "/info": 
                    await Info(botClient, update, cancellationToken);
                    break;
                case "/addphrase": 
                    await AddPhrase(botClient, update, cancellationToken);
                    break;
                case "/removephrase": 
                    await RemovePhrase(botClient, update, cancellationToken);
                    break;
                  case "/practice": 
                    await PracticePhrase(botClient, update, cancellationToken);
                    break;
                case "/edit":
                    await EditPhrase(botClient, update, cancellationToken);
                    break;
                case "/show":
                    await ShowAllPhrases(botClient, update, cancellationToken);
                    break;
                default:
                    await botClient.SendMessage(update.Message.Chat, $"Введите одну из предложенных команд: ");
                    throw new ArgumentException("Введите одну из предложенных команд!");
            }
        }
        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
             Console.WriteLine($"HandleError: {exception}, source: {source}");

            return;
        }

        private async Task Start(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            string userName = update.Message.From.Username ?? "User";
            long userID = update.Message.From.Id;
            var chatId = update.Message.Chat.Id;

            var existingUser = await _userService.GetUserbyId(userID);
            if (existingUser == null)
            {
                var user = new user
                {
                    TelegramUserId = userID,
                    TelegramUserName = userName,
                    RegisteredAt = DateTime.UtcNow,
                };
                await _userService.RegisterUserAsync(user, cancellationToken);

                await botClient.SendMessage(update.Message.Chat, $"Пользователь зарегистрирован, '{userName}'");
            }
            else {
                var phrases = _phraseService.GetAllByUserId(userID);

                if (!phrases.Any())
                {
                    await botClient.SendMessage(
                        chatId,
                        "У вас пока нет сохранённых фраз. Добавьте их командой /add",
                        cancellationToken: cancellationToken);
                }
                else
                {
                    var sb = new StringBuilder("Ваши фразы:\n\n");
                    foreach (var phrase in phrases)
                    {
                        sb.AppendLine($"{phrase.EnglishText} - {phrase.RussianText}");
                    }

                    await botClient.SendMessage(chatId, sb.ToString(), cancellationToken: cancellationToken);
                }

                await botClient.SendMessage(
                    chatId,
                    "Что вы хотите сделать?\n" +
                    "/addphrase — добавить фразу\n" +
                    "/edit — редактировать фразу\n" +
                    "/removephrase - удалить фразу\n"+
                    "/practice — начать практику",
                    cancellationToken: cancellationToken);
            }
        
        }

            static async Task Help(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            { 
                await botClient.SendMessage(update.Message.Chat, $" чтобы начать работу, введите команду /start "
                     + "Чтобы добавить новую фразу введите /addphrase, чтобы удалить фразу, введите /removephrase, чтобы практиковать изучаемые фразы, введите /practice");
            }
            static async Task Info(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                await botClient.SendMessage(update.Message.Chat, $"Версия 1.1.3. Создано 24.02.2025. By helga_tsrk");
            }

        public async Task AddPhrase(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update?.Message == null || update.Message.From == null)
                return;

            var chatId = update.Message.Chat.Id;
            long telegramUserId = update.Message.From.Id;

            if (!waitText || currentCommand != "/addphrase")
            {
                currentCommand = "/addphrase";
                waitText = true;

                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Введите английскую фразу, затем перевод через запятую (пример: see, понимать). " +
                          $"Чтобы отменить — введите {ExitPracticeCommand}.",
                    cancellationToken: cancellationToken);
                return;
            }

            string inputText = update.Message.Text?.Trim() ?? string.Empty;

            if (string.Equals(inputText, ExitPracticeCommand, StringComparison.OrdinalIgnoreCase))
            {
                currentCommand = null;
                waitText = false;
                await botClient.SendMessage(chatId, "Добавление фразы отменено.", cancellationToken: cancellationToken);
                return;
            }

            var parts = inputText.Split(new[] { ',' }, 2);

            if (parts.Length != 2)
            {
                 await botClient.SendMessage(
                    chatId: chatId,
                    text: "Некорректный ввод. Введите в формате: Английская фраза, Русский перевод (например: see, понимать).",
                    cancellationToken: cancellationToken);
               
                return;
            }

            string englishText = parts[0].Trim();
            string russianText = parts[1].Trim();

            if (string.IsNullOrWhiteSpace(englishText) || string.IsNullOrWhiteSpace(russianText))
            {
                await botClient.SendMessage(chatId, "Обе части фразы должны быть заполнены. Попробуйте ещё раз.", cancellationToken: cancellationToken);
                return;
            }

            var userPhrases = _phraseService.GetAllByUserId(telegramUserId) ?? Enumerable.Empty<Phrase>();
            bool alreadyExists = userPhrases.Any(p =>
                string.Equals(p.EnglishText?.Trim(), englishText, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.RussianText?.Trim(), russianText, StringComparison.OrdinalIgnoreCase)
            );

            if (alreadyExists)
            {
                await botClient.SendMessage(chatId, "Такая фраза уже существует.", cancellationToken: cancellationToken);
                currentCommand = null;
                waitText = false;
                return;
            }

            try
            {
                _phraseService.AddPhrase(telegramUserId, englishText, russianText);

                await botClient.SendMessage(
                    chatId: chatId,
                    text: $"Фраза добавлена: '{englishText}' — '{russianText}'",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            catch (InvalidOperationException invEx)
            {
               
                await botClient.SendMessage(chatId, invEx.Message, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                
                await botClient.SendMessage(chatId, "Произошла ошибка при сохранении фразы. Попробуйте позже.", cancellationToken: cancellationToken);
                Console.WriteLine($"Error saving phrase: {ex}");
            }
            finally
            {
                currentCommand = null;
                waitText = false;
            }
        }

        async Task RemovePhrase(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                currentCommand = "/removephrase";
                if (!waitText)
                {
                     await botClient.SendMessage(
                     chatId: update.Message.Chat,
                     text: $"Чтобы отменить команду удаления, введите {ExitPracticeCommand}",
                     cancellationToken: cancellationToken);
                var allPhrases = _phraseService.GetAllByUserId(update.Message.From.Id);
                    if (allPhrases.Count == 0) 
                    {
                    await botClient.SendMessage(update.Message.Chat,
                    $"У вас нет фраз для удаления!"
                    );
                    return;
                    }
                    var message = new StringBuilder("Ваши фразы:\n\n");
                    foreach (var phrase in allPhrases)
                    {
                        message.AppendLine($"▫️ {phrase.EnglishText} - {phrase.RussianText} [ID: {phrase.Id}]");
                        if (phrase.Id % 5 == 0) message.AppendLine("\n――――――――――\n"); 
                    }
                    const int maxLength = 4096;
                    var fullMessage = message.ToString();

                    for (int i = 0; i < fullMessage.Length; i += maxLength)
                    {
                        await botClient.SendMessage(
                            chatId: update.Message.Chat.Id,
                            text: fullMessage.Substring(i, Math.Min(maxLength, fullMessage.Length - i)),
                            cancellationToken: cancellationToken);
                    }
                    await botClient.SendMessage(update.Message.Chat, $"Введите ID фразы для удаления");
                    waitText = true;
                    return;
                }
                string? inputText = update.Message.Text.Trim();
                //var phrasesToDelete = _phraseService.FindPhrase(update.Message.From.Id, inputText).ToList();

                {
                    
                    if (update.Message?.Text == null)
                    {
                        await botClient.SendMessage(
                            chatId: update.Message.Chat.Id,
                            text: "Введите ID фразы для удаления.",
                            cancellationToken: cancellationToken);
                        return;
                    }

                    if (!long.TryParse(update.Message.Text, out long phraseId))
                    {
                        await botClient.SendMessage(
                            chatId: update.Message.Chat.Id,
                            text: "Неверный формат ID. Отправьте числовой ID фразы.",
                            cancellationToken: cancellationToken);
                        return;
                    }

                    
                    _phraseService.RemovePhrase(phraseId, update.Message.From.Id);

                    await botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: $"Фраза с ID {phraseId} успешно удалена.",
                        cancellationToken: cancellationToken);
                    currentCommand = null;
                    waitText = false;
                }
            }
        

        public async Task ShowAllPhrases(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message?.From == null)
            {
                await botClient.SendMessage(
                    chatId: update.Message?.Chat.Id ?? 0,
                    text: "Не удалось определить пользователя",
                    cancellationToken: cancellationToken);
                return;
            }

            long userId = update.Message.From.Id;

            try
            {
                var phrases = _phraseService.GetAllByUserId(userId);

                if (!phrases.Any())
                {
                    await botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: "У вас пока нет сохраненных фраз",
                        cancellationToken: cancellationToken);
                    return;
                }
                var message = new StringBuilder("Ваши фразы:\n\n");

                foreach (var phrase in phrases)
                {
                    message.AppendLine($"▫️ {phrase.EnglishText} - {phrase.RussianText} [ID: {phrase.Id}]");
                    if (phrase.Id % 5 == 0) message.AppendLine("\n――――――――――\n"); 
                }

                const int maxLength = 4096;
                var fullMessage = message.ToString();

                for (int i = 0; i < fullMessage.Length; i += maxLength)
                {
                    await botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: fullMessage.Substring(i, Math.Min(maxLength, fullMessage.Length - i)),
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(
                chatId: update.Message.Chat.Id,
                text: "Произошла ошибка при загрузке ваших фраз. Пожалуйста, попробуйте позже.",
                cancellationToken: cancellationToken);
            }
        }

        public async Task EditPhrase(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            long userId = update.Message.From.Id;
            var chatId = update.Message.Chat.Id;
            var phrases = _phraseService.GetAllByUserId(userId);
            //currentCommand = "/edit";

        await botClient.SendMessage(
                chatId: update.Message.Chat,
                text: $"Чтобы отменить команду редактирования, введите {ExitPracticeCommand}",
                cancellationToken: cancellationToken);
            try
            {
                if (!phrases.Any())
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "У вас пока нет сохраненных фраз",
                        cancellationToken: cancellationToken);
                    return;
                }

                var message = new StringBuilder("Ваши фразы:\n\n");
                foreach (var phrase in phrases)
                {
                    message.AppendLine($"{phrase.EnglishText} - {phrase.RussianText} [ID: {phrase.Id}]");
                    if (phrase.Id % 5 == 0) message.AppendLine("\n――――――――――\n");
                }


                await botClient.SendMessage(
                    chatId: chatId,
                    text: message.ToString(),
                    cancellationToken: cancellationToken);

                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Введите ID фразы, которую хотите отредактировать:",
                    cancellationToken: cancellationToken);

                _isEditingPhrase = true;
                _editPhraseId = 0;

            }

            catch (Exception ex)
            {                
                _isEditingPhrase = false;
                _editPhraseId = 0;

                await botClient.SendMessage(
                    chatId: chatId,
                    text: $"Ошибка при загрузке фраз: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task SaveEditedPhrase(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var chatId = update.Message.Chat.Id;
            var userId = update.Message.From.Id;
            string input = update.Message.Text.Trim();

            
            if (_editPhraseId == 0)
            {
                if (!long.TryParse(input, out long phraseId))
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Введите корректный числовой ID:",
                        cancellationToken: cancellationToken);
                    return;
                }

                var phrase = _phraseService.GetPhrase(phraseId);
                if (phrase == null)
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Фраза с таким ID не найдена.",
                        cancellationToken: cancellationToken);
                    _isEditingPhrase = false;
                    return;
                }

                _editPhraseId = phraseId;

                await botClient.SendMessage(
                    chatId: chatId,
                    text: $"Введите новый вариант в формате: Английский - Русский (используйте тире)",
                    cancellationToken: cancellationToken);
            }
            else
            {
               
                var parts = input.Split('-');
                if (parts.Length != 2)
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Введите фразу в формате: Английский - Русский (используйте тире)",
                        cancellationToken: cancellationToken);
                    return;
                }

                string english = parts[0].Trim();
                string russian = parts[1].Trim();

                await _phraseService.UpdatePhrase(update.Message.From.Id, _editPhraseId, english, russian);

                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Фраза успешно обновлена!",
                    cancellationToken: cancellationToken);

                
                _isEditingPhrase = false;
                _editPhraseId = 0;
            }
        }
       public async Task PracticePhrase(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var chatId = update.Message.Chat.Id;
            var userId = update.Message.From.Id;

            currentCommand = "/practice";

            await botClient.SendMessage(
                chatId: chatId,
                text: $"Чтобы завершить практику, введите {ExitPracticeCommand}",
                cancellationToken: cancellationToken);

            var phrases = _phraseService.GetAllByUserId(userId);
            if (!phrases.Any())
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Нет фраз для практики",
                    cancellationToken: cancellationToken);
                return;
            }

            var randomPhrase = phrases[new Random().Next(phrases.Count)];
            var session = new PhrasePracticeSession
            {
                CurrentPhrase = randomPhrase,
                AttemptsLeft = 3
            };

            if (randomPhrase.IsOneWord)
            {
                var translateToRussian = new Random().Next(2) == 0; 
                session.IsTranslatingToRussian = translateToRussian;

                session.CorrectAnswer = translateToRussian
                    ? randomPhrase.RussianText   
                    : randomPhrase.EnglishText;

                await botClient.SendMessage(
                    chatId: chatId,
                    text: translateToRussian
                        ? $"Переведите на русский: \"{randomPhrase.EnglishText}\""
                        : $"Переведите на английский: \"{randomPhrase.RussianText}\"",
                    replyMarkup: new ForceReplyMarkup { Selective = true },
                    cancellationToken: cancellationToken);
            }
            else
            {
                var words = randomPhrase.EnglishText.Split(' ');
                session.HiddenPart = string.Join(" ", words.Skip(1));

                session.CorrectAnswer = session.HiddenPart; 

                await botClient.SendMessage(
                    chatId: chatId,
                    text: $"Закончите фразу: \"{words[0]} ...\" - {randomPhrase.RussianText}",
                    replyMarkup: new ForceReplyMarkup { Selective = true },
                    cancellationToken: cancellationToken);
            }

            _practiceSessions[userId] = session;
            waitText = true;
        }

        private async Task HandlePracticeResponse(ITelegramBotClient botClient, Update update, PhrasePracticeSession session, CancellationToken cancellationToken)
        {
            var chatId = update.Message.Chat.Id;
            var userId = update.Message.From.Id;

            
            string Normalize(string s) =>
                string.Join(" ", s.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                      .ToLowerInvariant();

            var userAnswer = Normalize(update.Message.Text);
            var correctAnswer = Normalize(session.CorrectAnswer);
            var isCorrect = false;

            isCorrect = userAnswer == correctAnswer;
 

            if (isCorrect)
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Правильно! Молодец!",
                    cancellationToken: cancellationToken);

                _practiceSessions.Remove(userId);
                waitText = false;

                await PracticePhrase(botClient, update, cancellationToken);
            }
            else
            {
                session.AttemptsLeft--;

                if (session.AttemptsLeft > 0)
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"Неверно. Осталось попыток: {session.AttemptsLeft}",
                        replyMarkup: new ForceReplyMarkup { Selective = true },
                        cancellationToken: cancellationToken);
                }
                else
                {
                     correctAnswer = session.CurrentPhrase.IsOneWord
                        ? $"Правильный ответ: {session.CurrentPhrase.EnglishText} - {session.CurrentPhrase.RussianText}"
                        : $"Полная фраза: {session.CurrentPhrase.EnglishText} - {session.CurrentPhrase.RussianText}";

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"Исчерпаны все попытки. {correctAnswer}",
                        cancellationToken: cancellationToken);

                    _practiceSessions.Remove(userId);
                    waitText = false;
                    await PracticePhrase(botClient, update, cancellationToken);
                }
            }
        }
      
    }
}
 
