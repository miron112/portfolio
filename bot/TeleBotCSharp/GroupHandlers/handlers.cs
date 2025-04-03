//https://telegrambots.github.io/book/2/forward-copy-delete.html

using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using System.Collections.Concurrent;
using System.Threading;
using TeleBot;

namespace TeleBot
{
    public class Handlers
    {

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                // Обработка сообщений
                if (update.Message is { } message)
                {
                    var chat = message.Chat;
                    await ProcessChatUpdate(botClient, update, chat.Type, cancellationToken);
                }
                // Обработка колбэков
                else if (update.CallbackQuery is { } callbackQuery)
                {
                    // Получаем тип чата из сообщения колбэка
                    if (callbackQuery.Message is { } callbackMessage)
                    {
                        var chat = callbackMessage.Chat;
                        await ProcessChatUpdate(botClient, update, chat.Type, cancellationToken);

                        // Отвечаем на колбэк (обязательно для уведомлений)
                        await botClient.AnswerCallbackQuery(
                            callbackQueryId: callbackQuery.Id,
                            cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    Console.WriteLine("Неизвестный тип обновления");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки обновления: {ex}");
            }
        }

        private static async Task ProcessChatUpdate(ITelegramBotClient botClient, Update update, ChatType chatType, CancellationToken cancellationToken)
        {
            switch (chatType)
            {
                case ChatType.Private:
                    await HandlerPrivat.HandleUpdateAsync(botClient, update, cancellationToken);
                    break;

                case ChatType.Group or ChatType.Supergroup:
                    await HandlerGroup.HandleUpdateAsync(botClient, update, cancellationToken);
                    break;

                case ChatType.Channel:
                    // Проверка прав администратора
                    var admins = await botClient.GetChatAdministratorsAsync(update.Message.Chat.Id, cancellationToken);
                    if (admins.Any(a => a.User.Id == botClient.BotId))
                    {
                        // Обработка для канала
                    }
                    break;

                default:
                    Console.WriteLine($"Неизвестный тип чата: {chatType}");
                    break;
            }
        }
        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}





//<b> ...</b> Жирный текст

//<i>...</i> Курсив

//<u>...</u> Подчёркнутый текст

//<s>...</s> Зачёркнутый текст

//<code>...</code> Инлайн - код(моноширинный шрифт)

//<pre> ...</pre> Блок кода с сохранением форматирования

//<a>...</a>Ссылка<a href="url">Текст</a>

//<tg-spoiler>...</tg-spoiler>Спойлер (скрытый текст)

//<q> — строчный элемент для коротких цитат внутри предложения

//<blockquote> — блочный элемент для выделения многострочных цитат