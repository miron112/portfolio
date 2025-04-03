using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using System.Collections.Concurrent;
using System.Threading;
using TeleBot;
using static System.Net.Mime.MediaTypeNames;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.VisualBasic;

namespace TeleBot
{
    public class HandlerPrivat
    {
        public static DateTime currentTime = DateTime.Now;

        public static class TempStorage
        {
            private static readonly ConcurrentDictionary<long, object> _storage = new();

            public static void Add(long chatId, object data)
            {
                _storage[chatId] = data;
            }

            public static object Get(long chatId)
            {
                _storage.TryGetValue(chatId, out var data);
                return data;
            }

            public static void Update(long chatId, object data)
            {
                _storage[chatId] = data;
            }

            public static void Remove(long chatId)
            {
                _storage.TryRemove(chatId, out _);
            }
        }

        // Определяем возможные состояния
        public enum BotState
        {
            Registered,
            UnRegistered,
            WaitingPhone,
            WaitingNameNewUser,
            WaitingPhoneNewUser,
            WaitingRoleNewUser,

        }

        // Храним состояния пользователей (chatId - состояние)
        public static readonly ConcurrentDictionary<long, BotState> _userStates = new();

        // Хранение истории меню
        private static readonly ConcurrentDictionary<long, Stack<string>> _menuHistory = new();

        //Обновление истории меню
        private static Task UpdateMenuHistory(long chatId, string menuName)
        {
            if (!_menuHistory.ContainsKey(chatId))
            {
                _menuHistory[chatId] = new Stack<string>();
            }

            // Добавляем в историю только если это новый раздел
            if (_menuHistory[chatId].Count == 0 || _menuHistory[chatId].Peek() != menuName)
            {
                _menuHistory[chatId].Push(menuName);
            }
            return Task.CompletedTask;
        }

        //Возвращение в прошлое меню
        private static async Task HandleBackButton(ITelegramBotClient botClient, CallbackQuery query, CancellationToken cancellationToken, Update update)
        {
            var chatId = query.Message.Chat.Id;

            // Получаем историю меню
            if (!_menuHistory.TryGetValue(chatId, out var history) || history.Count == 0)
            {
                await ShowMainMenu(botClient, chatId, query.Message.MessageId, cancellationToken, update);
                return;
            }

            // Удаляем текущее меню
            history.Pop();

            // Если история пуста - показываем главное меню
            if (history.Count == 0)
            {
                await ShowMainMenu(botClient, chatId, query.Message.MessageId, cancellationToken, update);
                return;
            }

            // Получаем предыдущее меню
            var previousMenu = history.Peek();

            switch (previousMenu)
            {
                case "main":
                    await ShowMainMenu(botClient, chatId, query.Message.MessageId, cancellationToken, update);
                    break;
                case "profile":
                    await ShowProfileMenu(botClient, chatId, query.Message.MessageId, cancellationToken, update);
                    break;
                case "anonim_menu":
                    await ShowAnonimMenu(botClient, chatId, query.Message.MessageId, cancellationToken);
                    break;
            }
        }

        // Обработка сообщений и колбеков
        static public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var msg = update.Message;
            var query = update.CallbackQuery;


            if (update.Type == UpdateType.Message && msg != null)
            {
                await HandleMessage(botClient, msg, cancellationToken, update);
            }
            else if (update.Type == UpdateType.CallbackQuery && query != null)
            {
                await HandleCallbackQuery(botClient, query, cancellationToken, update);
            }
            else
            {
                Console.WriteLine($"Пользователь отправил что-то левое");
            }
        }

        //Обработка сообщений
        static private async Task HandleMessage(ITelegramBotClient botClient, Message msg, CancellationToken cancellationToken, Update update)
        {
            var chatId = msg.Chat.Id;
            var userMessageId = msg.MessageId;

            var user = msg.From;

            // Получаем текущее состояние пользователя
            var currentState = _userStates.GetValueOrDefault(msg.From.Id, BotState.Registered);

            Console.WriteLine($"");
            Console.WriteLine($"[{currentTime}] \nПользователь {user} с состоянием: {currentState} отправил: {msg.Text}");

            // Добавьте обработку контакта
            if (currentState == BotState.WaitingPhone && msg.Contact != null)
            {
                await botClient.SendMessage(
                        chatId: chatId,
                        text: $"Обрыботка вашего номера...",
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);

                if (msg.Contact.UserId == msg.From.Id)
                {
                    int answer = Backcode.UserPhoneRegistered(msg.Contact.PhoneNumber, Convert.ToString(msg.From.Id));
                    if (answer == 1)
                    {
                        await botClient.SendMessage(
                        chatId: chatId,
                        text: $"✅ Контакт принят: +{msg.Contact.PhoneNumber}\nИспользуйте /start для продолжения",
                        replyMarkup: Keyboards.Back,
                        cancellationToken: cancellationToken);
                        // Сброс состояния после успешной проверки

                        _userStates[msg.From.Id] = BotState.Registered;
                    }
                    else if(answer == 2)
                    {
                        await botClient.SendMessage(
                        chatId: chatId,
                        text: $"⚠️ Контакт принят: +{msg.Contact.PhoneNumber}\nНО вас не удалось авторизировать",
                        replyMarkup: Keyboards.Back,
                        cancellationToken: cancellationToken);
                    }
                    else if (answer == 0)
                    {
                        await botClient.SendMessage(
                        chatId: chatId,
                        text: $"❌ Контакт: +{msg.Contact.PhoneNumber}\nНе зарегистрирован в базе!",
                        replyMarkup: Keyboards.Back,
                        cancellationToken: cancellationToken);
                    }
                 
                }
                else
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "❌ Ошибка: Попытка отправить чужой контакт!",
                        replyMarkup: Keyboards.Back,
                        cancellationToken: cancellationToken);
                }
                return; // Выходим после обработки контакта
            }


            if(currentState == BotState.WaitingPhoneNewUser && msg != null)
                {
                // Сохраняем имя и переходим к следующему шагу
                string newUserName = msg.Text;
                // Можно добавить валидацию имени здесь

                // Сохраняем имя во временное хранилище
                // Вы можете использовать словарь для хранения временных данных
                TempStorage.Add(chatId, new { Name = newUserName });

                _userStates[msg.From.Id] = BotState.WaitingPhoneNewUser;
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Введите номер телефона нового пользователя:",
                    cancellationToken: cancellationToken
                );
                return;
            }

            if (currentState == BotState.WaitingNameNewUser && msg != null)
            {
                // Получаем ранее сохраненные данные
                var tempData = TempStorage.Get(chatId);
                string newUserName = tempData.Name;
                string newUserPhone = msg.Text;

                // Валидация номера телефона
                if (!IsValidPhoneNumber(newUserPhone))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Некорректный номер телефона. Попробуйте снова.",
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // Обновляем временное хранилище
                TempStorage.Update(chatId, new { Name = newUserName, Phone = newUserPhone });

                _userStates[msg.From.Id] = BotState.WaitingRoleNewUser;
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Выберите роль нового пользователя:",
                    replyMarkup: Keyboards.UserRoles, // Создайте клавиатуру с ролями
                    cancellationToken: cancellationToken
                );
                return;
            }

            if (currentState == BotState.WaitingRoleNewUser && msg != null)
                {
                // Начинаем процесс добавления нового пользователя

                // Получаем ранее сохраненные данные
                var tempData = TempStorage.Get(chatId);
                string newUserName = tempData.Name;
                string newUserPhone = tempData.Phone;
                string newUserRole = msg.Text;

                // Здесь сохраняем пользователя в базу данных
                SaveNewUserToDatabase(newUserName, newUserPhone, newUserRole);

                // Сбрасываем состояние
                _userStates[msg.From.Id] = BotState.Registered;
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Пользователь {newUserName} успешно добавлен!",
                    replyMarkup: Keyboards.AdminMenu,
                    cancellationToken: cancellationToken
                );
                return;


                switch (msg.Text)
            {
                
                case "/start":
                    // Очищаем историю и добавляем главное меню
                    _menuHistory[chatId] = new Stack<string>();
                    _menuHistory[chatId].Push("main");
                    if (Backcode.UserTgIdRegistered(Convert.ToString(user.Id)))
                    {
                        await ShowMainMenu(botClient, chatId, null, cancellationToken, update);
                        _userStates[msg.From.Id] = BotState.Registered;
                    }
                    else
                    {
                        await ShowAnonimMenu(botClient, chatId, null, cancellationToken);
                        _userStates[msg.From.Id] = BotState.UnRegistered;
                    }
                    
                    break;

                default:
                    await botClient.SendMessage(
                    chatId: msg.Chat.Id,
                    text: $"Вы написали: {msg.Text}",
                    cancellationToken: cancellationToken
                    );
                    break;
            } //конец



            // Пытаемся получить стек меню для пользователя с chatId = 123456
            if (_menuHistory.TryGetValue(msg.Chat.Id, out Stack<string> userMenuStack))
            {
                // Если стек существует — выводим историю
                Console.WriteLine($"История меню пользователя: {user}");
                foreach (string menu in userMenuStack.Reverse()) // Reverse() чтобы показать от старого к новому
                {
                    Console.Write($" - [{menu}]");
                }
                Console.WriteLine($"");
            }
            else
            {
                // Если история пуста
                Console.WriteLine("История меню отсутствует.");
            }

        }

        //Обработка колбеков
        static private async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery query, CancellationToken cancellationToken, Update update)
        {
            var user = query.From;
            var chatId = query.Message.Chat.Id;

            Console.WriteLine($"");
            Console.WriteLine($"[{currentTime}] \nПользователь {user} отправил колбек: {query.Data}");

            switch (query.Data)
            {
                case "authorization":
                    await ShowAuthorizationMenu(botClient, chatId, query.Message.MessageId, cancellationToken, update);
                    await UpdateMenuHistory(query.Message.Chat.Id, "authorization"); ;
                    break;

                case "profile_menu":
                    await ShowProfileMenu(botClient, chatId, query.Message.MessageId, cancellationToken, update);
                    await UpdateMenuHistory(query.Message.Chat.Id, "profile_menu"); ;
                    break;

                case "tasks_menu":
                    await ShowTasksMenu(botClient, chatId, query.Message.MessageId, cancellationToken, update);
                    await UpdateMenuHistory(query.Message.Chat.Id, "tasks_menu"); ;
                    break;

                case "community_menu":
                    await ShowСommunityMenu(botClient, chatId, query.Message.MessageId, cancellationToken, update);
                    await UpdateMenuHistory(query.Message.Chat.Id, "community_menu"); ;
                    break;

                case "offers_menu":
                    await ShowOffersMenu(botClient, chatId, query.Message.MessageId, cancellationToken, update);
                    await UpdateMenuHistory(query.Message.Chat.Id, "community_menu"); ;
                    break;

                case "admin_menu":
                    await ShowAdminMenu(botClient, chatId, query.Message.MessageId, cancellationToken, update);
                    await UpdateMenuHistory(query.Message.Chat.Id, "admin_menu"); ;
                    break;

                case "back":
                    await HandleBackButton(botClient, query, cancellationToken, update);
                    break;
                
                case "add_user":

                    _userStates[query.From.Id] = BotState.WaitingNameNewUser;
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Введите ФИО нового пользователя:",
                        cancellationToken: cancellationToken
                    );
                    break;

                default:
                    await botClient.AnswerCallbackQuery(query.Id);
                    break;
            }


            // Пытаемся получить стек меню для пользователя с chatId = 123456
            if (_menuHistory.TryGetValue(query.Message.Chat.Id, out Stack<string> userMenuStack))
            {
                // Если стек существует — выводим историю
                Console.WriteLine($"История меню пользователя: {user}");
                foreach (string menu in userMenuStack.Reverse()) // Reverse() чтобы показать от старого к новому
                {
                    Console.Write($" - [{menu}]");
                }
                Console.WriteLine($"");
            }
            else
            {
                // Если история пуста
                Console.WriteLine("История меню отсутствует.");
            }
        }

        //Методы для отображения менюшек
        
        
        //Меню до авторизации
        private static async Task ShowAnonimMenu(
            ITelegramBotClient botClient,
            long chatId,
            int? messageId,
            CancellationToken cancellationToken)
        {
            if (messageId.HasValue)
            {
                await botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId.Value,
                text: "🌟 <b>Добро пожаловать!</b> 🌟",
                parseMode: ParseMode.Html,
                replyMarkup: Keyboards.AnonimMenu,
                cancellationToken: cancellationToken
                );
            }
            else
            {
                await botClient.SendMessage(
                chatId: chatId,
                text: "🌟 <b>Добро пожаловать!</b> 🌟",
                parseMode: ParseMode.Html,
                replyMarkup: Keyboards.AnonimMenu,
                cancellationToken: cancellationToken
                );
            }
            _menuHistory[chatId] = new Stack<string>();
            _menuHistory[chatId].Push("anonim_menu");
        }

        //Меню авторизации
        private static async Task ShowAuthorizationMenu(
            ITelegramBotClient botClient,
            long chatId,
            int? messageId,
            CancellationToken cancellationToken,
            Update update)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "📲 <b>Отправьте свой контакт</b> 📲",
                parseMode: ParseMode.Html,
                replyMarkup: Keyboards.SharePhone, // Используем новую клавиатуру
                cancellationToken: cancellationToken
            );

            // Устанавливаем состояние ожидания контакта
            _userStates[update.CallbackQuery.From.Id] = BotState.WaitingPhone;
        }


        //Главное меню
        private static async Task ShowMainMenu(
        ITelegramBotClient botClient,
        long chatId,
        int? messageId,
        CancellationToken cancellationToken,
        Update update)
        {
            var markup = Keyboards.MainMenu;

            if (messageId.HasValue)
            {
                string username;
                User user = Backcode.GetUserById(Convert.ToString(update.CallbackQuery.From.Id));
                if (user == null)
                {
                    username = "Ошибка базы";
                }
                else
                {
                    username = user.FullName;
                }
                var text = $"🌟 <b>Добро пожаловать!</b> 🌟\n{username}";
                await botClient.EditMessageText(
                    chatId: chatId,
                    messageId: messageId.Value,
                    text: text,
                    parseMode: ParseMode.Html,
                    replyMarkup: markup,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                string username;
                User user = Backcode.GetUserById(Convert.ToString(update.Message.From.Id));
                if (user == null)
                {
                    username = "Ошибка базы";
                }
                else
                {
                    username = user.FullName;
                }
                var text = $"🌟 <b>Добро пожаловать!</b> 🌟\n{username}";
                await botClient.SendMessage(
                    chatId: chatId,
                    text: text,
                    parseMode: ParseMode.Html,
                    replyMarkup: markup,
                    cancellationToken: cancellationToken
                );
            }

            // Обновляем историю
            _menuHistory[chatId] = new Stack<string>();
            _menuHistory[chatId].Push("main");
        }

        //Меню профиля
        private static async Task ShowProfileMenu(
            ITelegramBotClient botClient,
            long chatId,
            int? messageId,
            CancellationToken cancellationToken,
            Update update)
        {
            User user = Backcode.GetUserById(Convert.ToString(update.CallbackQuery.From.Id));
            
            //string userName = "Ошибка";
            //string userPoint = "Ошибка";
            //string userLevel = "Ошибка";
            //string userRole = "Ошибка";

            string text = $"Ваш профиль:\n\nФИО: {user.FullName}\nОчки: {user.Points}\nУровень: {user.Level}\nРоль: {user.Role}";

            await botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId.Value,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: Keyboards.Back,
                cancellationToken: cancellationToken
            );

            // Обновляем историю
            if (_menuHistory.TryGetValue(chatId, out var history))
            {
                history.Push("profile_menu");
            }
        }

        //Меню задачек
        private static async Task ShowTasksMenu(
            ITelegramBotClient botClient,
            long chatId,
            int? messageId,
            CancellationToken cancellationToken,
            Update update)
        {
            await botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId.Value,
                text: "Вы в меню задачек",
                parseMode: ParseMode.Html,
                replyMarkup: Keyboards.Back,
                cancellationToken: cancellationToken
            );

            // Обновляем историю
            if (_menuHistory.TryGetValue(chatId, out var history))
            {
                history.Push("tasks_menu");
            }
        }

        //Меню сообщества
        private static async Task ShowСommunityMenu(
            ITelegramBotClient botClient,
            long chatId,
            int? messageId,
            CancellationToken cancellationToken,
            Update update)
        {
            await botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId.Value,
                text: "Наше <code>комьюнити</code>: \n─────────\n<a href=\"https://t.me/SielomStartUp\">StartUp MEDIA</a>",
                parseMode: ParseMode.Html,
                replyMarkup: Keyboards.Back,
                cancellationToken: cancellationToken
            );

            // Обновляем историю
            if (_menuHistory.TryGetValue(chatId, out var history))
            {
                history.Push("community_menu");
            }
        }
        //Меню сообщества
        private static async Task ShowAdminMenu(
            ITelegramBotClient botClient,
            long chatId,
            int? messageId,
            CancellationToken cancellationToken,
            Update update)
        {
            await botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId.Value,
                text: "Админ Панель  \n─────────\n",
                parseMode: ParseMode.Html,
                replyMarkup: Keyboards.AdminMenu,
                cancellationToken: cancellationToken
            );

            // Обновляем историю
            if (_menuHistory.TryGetValue(chatId, out var history))
            {
                history.Push("admin_menu");
            }
        }

        

        //Меню предложений
        private static async Task ShowOffersMenu(
            ITelegramBotClient botClient,
            long chatId,
            int? messageId,
            CancellationToken cancellationToken,
            Update update)
        {
            await botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId.Value,
                text: "Наше <code>комьюнити</code>: \n─────────\n<a href=\"https://t.me/SielomStartUp\">StartUp MEDIA</a>",
                parseMode: ParseMode.Html,
                replyMarkup: Keyboards.Back,
                cancellationToken: cancellationToken
            );

            // Обновляем историю
            if (_menuHistory.TryGetValue(chatId, out var history))
            {
                history.Push("community_menu");
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