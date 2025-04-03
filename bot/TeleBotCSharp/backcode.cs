using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace TeleBot
{
    public class User
    {
        public long Id { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Username { get; set; }
        public int Points { get; set; }
        public int Level { get; set; }
        public string Role { get; set; }
    }

    public class Backcode
    {
        public static string connectionString = Constants.nsra_database;

        public static string GetUserMention(Telegram.Bot.Types.User user2)
        {
            
            if (user2 == null)
                return "пользователь";

            // Если есть username, используем его
            if (!string.IsNullOrEmpty(user2.Username))
                return $"@{user2.Username}";

            // Ссылка для пользователя без username (HTML-формат)
            return $"<a href=\"tg://user?id={user2.Id}\">{user2.Id}</a>";
        }

        //Проверка на наличие в базе Tg_id
        public static bool UserTgIdRegistered(string user_tg_id)
        {
            bool answer = false;
            //SQL
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand("SELECT * FROM users WHERE telegram_id = @telegram_id", connection))
                    {
                        cmd.Parameters.AddWithValue("telegram_id", user_tg_id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows) // Проверяем, есть ли записи
                            {
                                answer = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            return answer;
        }

        //Проверка на регистрацию номера пользователя в базе + изменение Tg_id в базе
        public static int UserPhoneRegistered(string user_phone, string user_tg_id)
        {
            int answer = 0;
            //SQL
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand("SELECT * FROM users WHERE phone_number = @phone_number", connection))
                    {
                        cmd.Parameters.AddWithValue("phone_number", user_phone);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows) // Проверяем, есть ли записи
                            {
                                answer = UpdateUserTelegramID(user_tg_id, user_phone);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            return answer;
        }

        //Если есть то меняем ID_telegrama
        public static int UpdateUserTelegramID (string newTelegramID, string phone_number)
        {
            int success = 2;
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    using (var cmd = new NpgsqlCommand("UPDATE users SET telegram_id = @newTelegramID WHERE phone_number = @phone_number", connection))
                    {
                        cmd.Parameters.AddWithValue("newTelegramID", newTelegramID);
                        cmd.Parameters.AddWithValue("phone_number", phone_number);

                        int rowsAffected = cmd.ExecuteNonQuery(); // Выполняем UPDATE
                        if (rowsAffected > 0)
                        {
                            success = 1;
                        }
                        else
                        {
                            success = 2;
                        }// Если обновилась хотя бы 1 строка — успех

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            return success;
        }

        //Получаем все данные пользователя
        public static User GetUserById(string user_tg_Id)
        {
            User user = null;
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // Первый запрос: получение пользователя
                    using (var cmd = new NpgsqlCommand("SELECT * FROM users WHERE telegram_id = @user_tg_Id", connection))
                    {
                        cmd.Parameters.AddWithValue("user_tg_Id", user_tg_Id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Сохраняем данные из первого DataReader
                                var userId = reader.GetInt64(reader.GetOrdinal("id"));
                                var fullName = reader.GetString(reader.GetOrdinal("full_name"));
                                var phoneNumber = reader.GetString(reader.GetOrdinal("phone_number"));
                                var points = reader.GetInt32(reader.GetOrdinal("points"));
                                var levelId = reader.GetInt32(reader.GetOrdinal("level_id"));
                                var roleId = reader.GetInt32(reader.GetOrdinal("role_id"));

                                // Теперь закрываем первый DataReader
                                reader.Close();

                                // Второй запрос: получение уровня
                                using (var cmd2 = new NpgsqlCommand("SELECT name FROM level WHERE id = @level_id", connection))
                                {
                                    cmd2.Parameters.AddWithValue("level_id", levelId);
                                    int? levelInt = Convert.ToInt32(cmd2.ExecuteScalar());
                                    int levelName = levelInt ?? 0;

                                    // Третий запрос: получение роли
                                    using (var cmd3 = new NpgsqlCommand("SELECT name FROM access_role WHERE id = @role_id", connection))
                                    {
                                        cmd3.Parameters.AddWithValue("role_id", roleId);
                                        var roleName = cmd3.ExecuteScalar() as string ?? "Ошибка";

                                        // Создаем объект User здесь, внутри блока cmd3
                                        user = new User
                                        {
                                            Id = userId,
                                            FullName = fullName,
                                            PhoneNumber = phoneNumber,
                                            Points = points,
                                            Level = levelName,
                                            Role = roleName
                                        };
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Не нашли данных пользователя {user_tg_Id}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка БД: {ex.Message}");
            }
            return user;
        }

        //Начисление баллов + проверка на повышение уровня
        public static async Task GivePoint(Telegram.Bot.Types.User userTo, int newValue, ITelegramBotClient botClient, long chatId)
        {
            string user_tg_Id = Convert.ToString(userTo.Id);
            string table = "users";
            string column = "points";
            int point = Convert.ToInt32(DataProgram.ReadUserData(table, "points", user_tg_Id));
            DataProgram.UpdateUserData(table, user_tg_Id, column, point + newValue);
            var oldLevelId = DataProgram.ReadUserData(table, "level_id", user_tg_Id);
            var oldLevel = DataProgram.ReadData("level", "name", oldLevelId);

            bool success = DataProgram.CheckAndUpdateLevel(user_tg_Id);
            if (success == true)
            {
                var newLevelId = DataProgram.ReadUserData(table, "level_id", user_tg_Id);
                var newLevel = DataProgram.ReadData("level", "name", newLevelId);

                string text = $"Пользователь {Backcode.GetUserMention(userTo)} повысил свой уровень!\n" +
                                $"{oldLevel} → {newLevel}";
                await HandlerGroup.SendNotification(botClient, chatId, text);
                
            }
        }
    }
}
