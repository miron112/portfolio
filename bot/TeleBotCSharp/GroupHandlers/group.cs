using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using static TeleBot.HandlerPrivat;
using TeleBot;
using Telegram.Bot.Types;
using static System.Net.Mime.MediaTypeNames;

namespace TeleBot
{


    public class HandlerGroup
    {

        public static string AdminRole = "Admin";
        public static string TeacherRole = "Teacher";


        static public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var msg = update.Message;
            var query = update.CallbackQuery;

            if (update.Type == UpdateType.Message && msg != null)
            {
                await HandleMessage(botClient, msg, cancellationToken);
            }
            else
            {
                Console.WriteLine($"Пользователь отправил что-то левое");
            }
        }

        static private async Task HandleMessage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var msg = message;
            var chatId = msg.Chat.Id;
            var user = msg.From;

            Console.WriteLine($"[{DateTime.Now}] \nПользователь {user?.Username ?? "без имени"} отправил: {msg.Text}");

            // Обработка команд
            if (msg.Text.StartsWith('/') && msg.Text !=null)
            {
                // Разбиваем команду на части (без ограничения на количество элементов)
                var commandParts = msg.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = commandParts[0].ToLower();

                switch (command)
                {
                    case "/gp":
                        await HandleGiveCommand(botClient, msg, chatId, commandParts, cancellationToken);
                        break;

                    default:
                        await botClient.SendMessage(
                            chatId,
                            "Неизвестная команда. Используйте /gp {количество} {причина}",
                            cancellationToken: cancellationToken);
                        break;
                }
                return;
            }
        }

        static public async Task HandleGiveCommand(ITelegramBotClient botClient, Message msg, long chatId, string[] commandParts, CancellationToken cancellationToken)
        {
            var dataUser = Backcode.GetUserById(Convert.ToString(msg.From.Id));
            if (dataUser == null)
            {
                // Пользователь не найден: создаем нового или сообщаем об ошибке
                dataUser = new User
                {
                    Id = -1,
                    FullName = "Гость",
                    Role = "Гость"
                    // Другие поля по умолчанию
                };
                Console.WriteLine("Пользователь не найден. Создан временный профиль.");
            }

            if (dataUser.Role == AdminRole || dataUser.Role == TeacherRole)
            {
                var dataUserTo = Backcode.GetUserById(Convert.ToString(msg.ReplyToMessage.From.Id));
                if (dataUserTo == null)
                {
                    string text = "❌ Пользователь, которому вы пытаетесь перевести баллы, не зарегистрирован.";
                    await SendAndDeleteError(botClient, chatId, text, cancellationToken, msg);
                    return;
                }

                // Проверка на ответное сообщение
                if (msg.ReplyToMessage == null)
                {
                    string text = "❌ Используйте команду /gp в ответ на сообщение пользователя.";
                    await SendAndDeleteError(botClient, chatId, text, cancellationToken, msg);
                    return;
                }

                // Проверка параметров
                if (commandParts.Length < 2 || !int.TryParse(commandParts[1], out int points) || points <= 0)
                {
                    await SendAndDeleteError(
                        botClient, chatId,
                        "❌ Неверный формат. Используйте: /gp {количество} {причина}",
                        cancellationToken, msg);
                    return;
                }

                // Поиск параметра "reason"
                string cause = string.Empty;
                if (commandParts.Length > 2)
                {
                    cause = string.Join(" ", commandParts.Skip(2));
                }

                // Удаляем исходную команду
                await botClient.DeleteMessage(chatId, msg.MessageId, cancellationToken);

                var userFrom = msg.From;
                var userTo = msg.ReplyToMessage.From;
                if (userTo == null)
                {
                    string text = "Ошибка: Пользователь не найден";
                    await SendAndDeleteError(botClient, chatId, text, cancellationToken, msg);
                    return;
                }

                var response = $"✅ Пользователю {Backcode.GetUserMention(userTo)} начислено <u><b>{points}</b></u> баллов \nОт {dataUser.FullName}";

                if (!string.IsNullOrEmpty(cause))
                {
                    response += $"\nПричина: <pre>{cause}</pre>";
                }
                // Отправляем подтверждение
                await botClient.SendMessage(chatId, response, ParseMode.Html, cancellationToken: cancellationToken);
                //await SendAndDeleteMessage(botClient, chatId, response, 5000, cancellationToken);

                // Здесь логика сохранения баллов в БД
                await Backcode.GivePoint(userTo, points, botClient, chatId);
            }
            else
            {
                var response = $"❌ Отказано в доступе";
                await SendAndDeleteError(botClient, chatId, response, cancellationToken, msg);
            }
        }

        public static async Task SendNotification(ITelegramBotClient botClient, long chatId, string text)
        {
            try
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: text,
                    ParseMode.Html
                );

                Console.WriteLine("Оповещение отправлено!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке сообщения: {ex.Message}");
            }
        }

        static private async Task SendAndDeleteError(ITelegramBotClient botClient, long chatId, string text, CancellationToken cancellationToken, Message msg)
        {
            await botClient.DeleteMessage(chatId, msg.MessageId, cancellationToken);
            var error = await botClient.SendMessage(chatId, text, ParseMode.Html, cancellationToken: cancellationToken);
            _ = Task.Delay(5000).ContinueWith(async _ =>
                await botClient.DeleteMessage(chatId, error.MessageId, cancellationToken));
        }

        static private async Task SendAndDeleteMessage(ITelegramBotClient botClient, long chatId, string text, int delayMs, CancellationToken cancellationToken)
        {
            var message = await botClient.SendMessage(chatId, text, ParseMode.Html, cancellationToken: cancellationToken);
            _ = Task.Delay(delayMs).ContinueWith(async _ =>
                await botClient.DeleteMessage(chatId, message.MessageId, cancellationToken));
        }
    }
}