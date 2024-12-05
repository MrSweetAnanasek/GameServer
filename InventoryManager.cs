using MySql.Data.MySqlClient;
using System;
using System.Net;

public class InventoryManager
{
    public static void HandleInventory(dynamic requestData, HttpListenerResponse response, string connectionString)
    {
        int userId = requestData.userid;
        string itemId = requestData.itemid;
        int quantity = requestData.quantity;
        string itemType = requestData.itemtype;
        bool isStackable = requestData.isstackable;
        bool isTradeable = requestData.istradeable;
        string action = requestData.action;

        using (var connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();

                switch (action.ToLower())
                {
                    case "add":
                        AddItemToInventory(connection, userId, itemId, quantity, itemType, isStackable, isTradeable, response);
                        break;
                    case "remove":
                        RemoveItemFromInventory(connection, userId, itemId, quantity, response);
                        break;
                    case "use":
                        UseItemInInventory(connection, userId, itemId, quantity, response);
                        break;
                    default:
                        Program.SendResponse(response, new { status = "error", message = "Invalid action type." });
                        return;
                }

                Program.SendResponse(response, new { status = "success", message = "Inventory operation successful." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Inventory operation error: {ex.Message}");
                Program.SendResponse(response, new { status = "error", message = "Inventory operation failed!" });
            }
        }
    }

    public static void AddItemToInventory(MySqlConnection connection, int userId, string itemId, int quantity, string itemType, bool isStackable, bool isTradeable, HttpListenerResponse response)
    {
        string checkItemQuery = "SELECT Quantity FROM Inventory WHERE UserID = @UserID AND ItemID = @ItemID";
        using (var cmd = new MySqlCommand(checkItemQuery, connection))
        {
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.Parameters.AddWithValue("@ItemID", itemId);

            var existingQuantity = cmd.ExecuteScalar();

            if (existingQuantity != DBNull.Value)
            {
                // If the item exists and is stackable, update the quantity
                int currentQuantity = Convert.ToInt32(existingQuantity);
                if (isStackable)
                {
                    string updateQuantityQuery = "UPDATE Inventory SET Quantity = @Quantity WHERE UserID = @UserID AND ItemID = @ItemID";
                    using (var updateCmd = new MySqlCommand(updateQuantityQuery, connection))
                    {
                        updateCmd.Parameters.AddWithValue("@Quantity", currentQuantity + quantity);
                        updateCmd.Parameters.AddWithValue("@UserID", userId);
                        updateCmd.Parameters.AddWithValue("@ItemID", itemId);
                        updateCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // Item is not stackable, cannot add more of the same item
                    Program.SendResponse(response, new { status = "error", message = "Item is not stackable, cannot add more." });
                    return;  // Early return, no further processing
                }
            }
            else
            {
                // If the item does not exist, add it to the inventory
                string insertQuery = "INSERT INTO Inventory (UserID, ItemID, Quantity, ItemType, IsStackable, IsTradeable) VALUES (@UserID, @ItemID, @Quantity, @ItemType, @IsStackable, @IsTradeable)";
                using (var insertCmd = new MySqlCommand(insertQuery, connection))
                {
                    insertCmd.Parameters.AddWithValue("@UserID", userId);
                    insertCmd.Parameters.AddWithValue("@ItemID", itemId);
                    insertCmd.Parameters.AddWithValue("@Quantity", quantity);
                    insertCmd.Parameters.AddWithValue("@ItemType", itemType);
                    insertCmd.Parameters.AddWithValue("@IsStackable", isStackable);
                    insertCmd.Parameters.AddWithValue("@IsTradeable", isTradeable);

                    insertCmd.ExecuteNonQuery();
                }
            }
        }
    }

    public static void RemoveItemFromInventory(MySqlConnection connection, int userId, string itemId, int quantity, HttpListenerResponse response)
    {
        string checkItemQuery = "SELECT Quantity FROM Inventory WHERE UserID = @UserID AND ItemID = @ItemID";
        using (var cmd = new MySqlCommand(checkItemQuery, connection))
        {
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.Parameters.AddWithValue("@ItemID", itemId);

            var existingQuantity = cmd.ExecuteScalar();

            if (existingQuantity != DBNull.Value)
            {
                int currentQuantity = Convert.ToInt32(existingQuantity);
                if (currentQuantity >= quantity)
                {
                    string updateQuery = "UPDATE Inventory SET Quantity = @Quantity WHERE UserID = @UserID AND ItemID = @ItemID";
                    using (var updateCmd = new MySqlCommand(updateQuery, connection))
                    {
                        updateCmd.Parameters.AddWithValue("@Quantity", currentQuantity - quantity);
                        updateCmd.Parameters.AddWithValue("@UserID", userId);
                        updateCmd.Parameters.AddWithValue("@ItemID", itemId);
                        updateCmd.ExecuteNonQuery();
                    }

                    // If the quantity reaches zero, remove the item
                    if (currentQuantity - quantity == 0)
                    {
                        string deleteQuery = "DELETE FROM Inventory WHERE UserID = @UserID AND ItemID = @ItemID";
                        using (var deleteCmd = new MySqlCommand(deleteQuery, connection))
                        {
                            deleteCmd.Parameters.AddWithValue("@UserID", userId);
                            deleteCmd.Parameters.AddWithValue("@ItemID", itemId);
                            deleteCmd.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    Program.SendResponse(response, new { status = "error", message = "Not enough items to remove." });
                }
            }
            else
            {
                Program.SendResponse(response, new { status = "error", message = "Item not found in inventory." });
            }
        }
    }

    public static void UseItemInInventory(MySqlConnection connection, int userId, string itemId, int quantity, HttpListenerResponse response)
    {
        string checkItemQuery = "SELECT Quantity FROM Inventory WHERE UserID = @UserID AND ItemID = @ItemID";
        using (var cmd = new MySqlCommand(checkItemQuery, connection))
        {
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.Parameters.AddWithValue("@ItemID", itemId);

            var existingQuantity = cmd.ExecuteScalar();

            if (existingQuantity != DBNull.Value)
            {
                int currentQuantity = Convert.ToInt32(existingQuantity);

                if (currentQuantity >= quantity)
                {
                    string updateQuery = "UPDATE Inventory SET Quantity = @Quantity WHERE UserID = @UserID AND ItemID = @ItemID";
                    using (var updateCmd = new MySqlCommand(updateQuery, connection))
                    {
                        updateCmd.Parameters.AddWithValue("@Quantity", currentQuantity - quantity);
                        updateCmd.Parameters.AddWithValue("@UserID", userId);
                        updateCmd.Parameters.AddWithValue("@ItemID", itemId);
                        updateCmd.ExecuteNonQuery();
                    }

                    // If the quantity reaches zero, remove the item
                    if (currentQuantity - quantity == 0)
                    {
                        string deleteQuery = "DELETE FROM Inventory WHERE UserID = @UserID AND ItemID = @ItemID";
                        using (var deleteCmd = new MySqlCommand(deleteQuery, connection))
                        {
                            deleteCmd.Parameters.AddWithValue("@UserID", userId);
                            deleteCmd.Parameters.AddWithValue("@ItemID", itemId);
                            deleteCmd.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    Program.SendResponse(response, new { status = "error", message = "Not enough items to use." });
                }
            }
            else
            {
                Program.SendResponse(response, new { status = "error", message = "Item not found in inventory." });
            }
        }
    }
}
