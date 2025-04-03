using System;
using System.Collections.Generic;
using Npgsql;

namespace TeleBot
{
    public static class DataProgram
    {
        private static readonly string _connectionString = Constants.nsra_database;

        // Проверка подключения к БД
        public static void BD_Connect()
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("База успешно подключена!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }
        }

        // Универсальный метод обновления данных
        public static void UpdateData(string table, string userId, string column, object newValue)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(column))
            {
                Console.WriteLine("Ошибка: Некорректное имя таблицы или столбца.");
                return;
            }

            string query = $"UPDATE \"{table}\" SET \"{column}\" = @value WHERE id = @id";

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@value", newValue);
                        command.Parameters.AddWithValue("@id", userId);

                        int rowsAffected = command.ExecuteNonQuery();
                        Console.WriteLine($"Обновлено {rowsAffected} строк(и).");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"(метод обновления) Ошибка: {ex.Message}");
            }
        }

        // 
        //private static void SaveUserToDatabase(string phone, string fullName, int role_id)
        //{

        //    //надо добавить строку с подключением
        //    using (var connection = new NpgsqlConnection(_connectionString))
        //    {
        //        connection.Open();

        //        // SQL-запрос для вставки данных в таблицу
        //        var sql = "INSERT INTO users (phone, full_name, role_id) VALUES (@phone, @fullName, @role_id)";
        //        using (var command = new NpgsqlCommand(sql, connection))
        //        {
        //            command.Parameters.AddWithValue("phone", phone);
        //            command.Parameters.AddWithValue("fullName", fullName);
        //            command.Parameters.AddWithValue("role_id", role_id);
                    

        //            command.ExecuteNonQuery();
        //        }
        //    }

        //    Console.WriteLine($"Пользователь с телефоном {phone} добавлен в базу данных.");
        //}


        // Универсальный метод обновления данных пользователя
        public static void UpdateUserData(string table, string userId, string column, object newValue)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(column))
            {
                Console.WriteLine("Ошибка: Некорректное имя таблицы или столбца.");
                return;
            }

            string query = $"UPDATE \"{table}\" SET \"{column}\" = @value WHERE telegram_id = @id";

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@value", newValue);
                        command.Parameters.AddWithValue("@id", userId);

                        int rowsAffected = command.ExecuteNonQuery();
                        Console.WriteLine($"Обновлено {rowsAffected} строк(и).");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"(метод обновления) Ошибка: {ex.Message}");
            }
        }

        // Универсальный метод запроса данных
        public static List<Dictionary<string, object>> ExecuteQuery(string sqlQuery, params NpgsqlParameter[] parameters)
        {
            var result = new List<Dictionary<string, object>>();

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(sqlQuery, connection))
                    {
                        if (parameters != null)
                            command.Parameters.AddRange(parameters);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.GetValue(i);
                                }
                                result.Add(row);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"(метод чтения) Ошибка: {ex.Message}");
            }

            return result;
        }

        //Универсальный метод чтения данных пользователей
        public static object ReadUserData(string table, string column, object value)
        {
            // Проверка корректности имени таблицы и столбца
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(column))
            {
                Console.WriteLine("Ошибка: Некорректное имя таблицы или столбца.");
                return null;
            }

            // Запрос на чтение данных
            string query = $"SELECT \"{column}\" FROM \"{table}\" WHERE telegram_id = @value";

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        // Добавляем параметр для предотвращения SQL инъекций
                        command.Parameters.AddWithValue("@value", value);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                return reader.GetValue(0);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
            }
            return null;
        }

        //Универсальный метод чтения данных
        public static object ReadData(string table, string column, object value)
        {
            // Проверка корректности имени таблицы и столбца
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(column))
            {
                Console.WriteLine("Ошибка: Некорректное имя таблицы или столбца.");
                return null;
            }

            // Запрос на чтение данных
            string query = $"SELECT \"{column}\" FROM \"{table}\" WHERE id = @value";

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        // Добавляем параметр для предотвращения SQL инъекций
                        command.Parameters.AddWithValue("@value", value);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                return reader.GetValue(0);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
                return null;
            }
            return null;
        }


        // Проверка на повышение уровня
        public static bool CheckAndUpdateLevel(string user_tg_Id)
        {
            bool success = false;

            string query = @"
        WITH next_level AS (
            SELECT id 
            FROM level 
            WHERE required_points <= (SELECT points FROM users WHERE telegram_id = @user_tg_Id)
            ORDER BY required_points DESC 
            LIMIT 1
        )
        UPDATE users
        SET level_id = (SELECT id FROM next_level)
        WHERE telegram_id = @user_tg_Id 
        AND level_id <> (SELECT id FROM next_level)
        RETURNING level_id;";  // Возвращаем новый уровень для подтверждения обновления

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@user_tg_Id", user_tg_Id);

                        // Выполняем запрос и проверяем, был ли возвращен новый уровень
                        var result = command.ExecuteScalar();

                        if (result != null)
                        {
                            success = true;
                            Console.WriteLine("Проверка уровня выполнена!");
                        }
                        else
                        {
                            Console.WriteLine("Уровень не был изменен.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"(проверка на повышение уровня) Ошибка: {ex.Message}");
            }

            return success;
        }

    }
}
