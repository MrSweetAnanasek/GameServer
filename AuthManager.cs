using System;
using MySql.Data.MySqlClient;
using System.Net;

public class AuthManager
{
    public static void HandleRegister(dynamic requestData, HttpListenerResponse response, string connectionString)
    {
        string displayName = requestData.displayname;
        string email = requestData.email;
        string password = requestData.password;
        string birthDate = requestData.birthdate;

        string passwordHash = HashPassword(password);

        using (var connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                string query = "INSERT INTO Users (DisplayName, Email, PasswordHash, BirthDate) VALUES (@DisplayName, @Email, @PasswordHash, @BirthDate)";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@DisplayName", displayName);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    cmd.Parameters.AddWithValue("@BirthDate", DateTime.Parse(birthDate));

                    cmd.ExecuteNonQuery();
                    Program.SendResponse(response, new { status = "success", message = $"Created player account {displayName}!" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Register account error: {ex.Message}");
                Program.SendResponse(response, new { status = "error", message = "Account registration failed!" });
            }
        }
    }

    public static void HandleLogin(dynamic requestData, HttpListenerResponse response, string connectionString)
    {
        string email = requestData.email;
        string password = requestData.password;
        string passwordHash = HashPassword(password);

        using (var connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM Users WHERE Email = @Email AND PasswordHash = @PasswordHash";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    if (count > 0)
                    {
                        Program.SendResponse(response, new { status = "success", message = $"Logged as: {email}!" });
                    }
                    else
                    {
                        Program.SendResponse(response, new { status = "error", message = "Invalid e-mail or password!" });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login failed: {ex.Message}");
                Program.SendResponse(response, new { status = "error", message = "Login Error" });
            }
        }
    }

    private static string HashPassword(string password)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
