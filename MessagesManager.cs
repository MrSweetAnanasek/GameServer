using MySql.Data.MySqlClient;
using System;
using System.Net;

public class MessagesManager
{
    public static void HandleMessages(dynamic requestData, HttpListenerResponse response, string connectionString)
    {
        int userId = requestData.userid ?? 0;
        int messageId = requestData.messageid ?? 0; // Used in case of actions like "mark_as_read" or "delete"
        string action = requestData.action;
        int recipientId = requestData.recipientid ?? 0; // For sending messages
        string subject = requestData.subject;
        string message = requestData.message;

        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Zmiana: odpowiedź jest teraz wysyłana tylko raz
                switch (action.ToLower())
                {
                    case "send":
                        SendMessage(connection, userId, recipientId, subject, message, response);
                        break;
                    case "get":
                        GetMessages(connection, userId, response);
                        break;
                    case "mark_as_read":
                        MarkMessageAsRead(connection, messageId, response);
                        break;
                    case "delete":
                        DeleteMessage(connection, userId, messageId, response);
                        break;
                    default:
                        Program.SendResponse(response, new { status = "error", message = "Invalid action type." });
                        return;
                }
            }

            Program.SendResponse(response, new { status = "success", message = "Message operation successful." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
            Program.SendResponse(response, new { status = "error", message = "Message operation failed!" });
        }
    }

    public static void SendMessage(MySqlConnection connection, int senderId, int recipientId, string subject, string message, HttpListenerResponse response)
    {
        try
        {
            string query = @"
            INSERT INTO Messages (SenderID, RecipientID, Subject, Message)
            VALUES (@SenderID, @RecipientID, @Subject, @Message)";

            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@SenderID", senderId);
                cmd.Parameters.AddWithValue("@RecipientID", recipientId);
                cmd.Parameters.AddWithValue("@Subject", subject);
                cmd.Parameters.AddWithValue("@Message", message);

                cmd.ExecuteNonQuery();
            }

            Program.SendResponse(response, new { status = "success", message = "Message sent successfully." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
            Program.SendResponse(response, new { status = "error", message = "Message sending failed!" });
        }
    }

    public static void GetMessages(MySqlConnection connection, int userId, HttpListenerResponse response)
    {
        try
        {
            List<dynamic> messages = new List<dynamic>();

            string query = "SELECT * FROM Messages WHERE RecipientID = @UserID ORDER BY SentTime DESC";

            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        messages.Add(new
                        {
                            MessageID = reader["MessageID"],
                            SenderID = reader["SenderID"],
                            RecipientID = reader["RecipientID"],
                            Subject = reader["Subject"],
                            Message = reader["Message"],
                            SentTime = reader["SentTime"],
                            IsRead = reader["IsRead"],
                            ReadTime = reader["ReadTime"]
                        });
                    }
                }
            }

            Program.SendResponse(response, new { status = "success", messages = messages });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving messages: {ex.Message}");
            Program.SendResponse(response, new { status = "error", message = "Failed to retrieve messages!" });
        }
    }

    public static void MarkMessageAsRead(MySqlConnection connection, int messageId, HttpListenerResponse response)
    {
        try
        {
            string query = @"
            UPDATE Messages
            SET IsRead = TRUE, ReadTime = CURRENT_TIMESTAMP
            WHERE MessageID = @MessageID";

            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@MessageID", messageId);
                cmd.ExecuteNonQuery();
            }

            Program.SendResponse(response, new { status = "success", message = "Message marked as read." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error marking message as read: {ex.Message}");
            Program.SendResponse(response, new { status = "error", message = "Failed to mark message as read!" });
        }
    }

    public static void DeleteMessage(MySqlConnection connection, int userId, int messageId, HttpListenerResponse response)
    {
        try
        {
            // First, we need to ensure that the user is the recipient or the sender of the message to allow deletion
            string checkQuery = "SELECT COUNT(*) FROM Messages WHERE MessageID = @MessageID AND (SenderID = @UserID OR RecipientID = @UserID)";

            using (var cmd = new MySqlCommand(checkQuery, connection))
            {
                cmd.Parameters.AddWithValue("@MessageID", messageId);
                cmd.Parameters.AddWithValue("@UserID", userId);

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                {
                    Program.SendResponse(response, new { status = "error", message = "You do not have permission to delete this message." });
                    return;
                }
            }

            // If user has permission, delete the message
            string deleteQuery = "DELETE FROM Messages WHERE MessageID = @MessageID";

            using (var cmd = new MySqlCommand(deleteQuery, connection))
            {
                cmd.Parameters.AddWithValue("@MessageID", messageId);
                cmd.ExecuteNonQuery();
            }

            Program.SendResponse(response, new { status = "success", message = "Message deleted successfully." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting message: {ex.Message}");
            Program.SendResponse(response, new { status = "error", message = "Failed to delete message!" });
        }
    }
}

