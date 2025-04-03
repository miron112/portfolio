using System;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleBot
{
    public static class Keyboards
    {
        public static InlineKeyboardMarkup MainMenu => new InlineKeyboardMarkup(new[]
        {
            new[]{InlineKeyboardButton.WithCallbackData("Профиль", "profile_menu"),},
            new[]{InlineKeyboardButton.WithCallbackData("Задачки", "tasks_menu")},
            new[]{InlineKeyboardButton.WithCallbackData("Комьюнити", "community_menu") },
            new[]{InlineKeyboardButton.WithCallbackData("Предложения", "offers_menu") },
            new[]{InlineKeyboardButton.WithCallbackData("Админ панель", "admin_menu") },
        });

        public static InlineKeyboardMarkup Back => new InlineKeyboardMarkup(new[]
        {
            new[]{InlineKeyboardButton.WithCallbackData("Назад", "back"),},
        });

        public static InlineKeyboardMarkup AdminMenu => new InlineKeyboardMarkup(new[]
                {
                    new[]{InlineKeyboardButton.WithCallbackData("Список пользователей", "list_users")},
                    new[]{InlineKeyboardButton.WithCallbackData("Добавить пользователя", "add_user")},
                    new[]{InlineKeyboardButton.WithCallbackData("Назад", "back") }
                });
        public static InlineKeyboardMarkup AnonimMenu => new InlineKeyboardMarkup(new[]
        {
            new[]{InlineKeyboardButton.WithCallbackData("Авторизироваться", "authorization")},
            new[]{InlineKeyboardButton.WithCallbackData("FAQ", "faq")},
            new[]{InlineKeyboardButton.WithCallbackData("Задать вопрос", "ask a question")},
        });

        public static ReplyKeyboardMarkup SharePhone => new ReplyKeyboardMarkup(new[]
        {
            new[] { KeyboardButton.WithRequestContact("📞 Отправить контакт") }
        })
        {
            ResizeKeyboard = true,  // Уменьшает клавиатуру
            OneTimeKeyboard = true   // Закрывает клавиатуру после нажатия
        };

    }
}
