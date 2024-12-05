using MySql.Data.MySqlClient;
using System;

public class DatabaseManager
{
    public static void EnsureTableAndColumnsExist(string connectionString)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            // --- Users Table Logic ---
            string tableCheckQuery = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'Users'";
            using (var cmd = new MySqlCommand(tableCheckQuery, connection))
            {
                int tableExists = Convert.ToInt32(cmd.ExecuteScalar());
                if (tableExists == 0)
                {
                    string createTableQuery = @"
                        CREATE TABLE Users (
                            Id INT AUTO_INCREMENT PRIMARY KEY,
                            DisplayName VARCHAR(100),
                            Email VARCHAR(100) UNIQUE,
                            PasswordHash VARCHAR(255),
                            BirthDate DATE
                        );";
                    using (var createCmd = new MySqlCommand(createTableQuery, connection))
                    {
                        createCmd.ExecuteNonQuery();
                        Console.WriteLine("The 'Users' table has been created.");
                    }
                }
            }

            // --- Fishdex Table Logic ---
            string fishdexTableCheckQuery = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'Fishdex'";
            using (var fishdexCmd = new MySqlCommand(fishdexTableCheckQuery, connection))
            {
                int fishdexTableExists = Convert.ToInt32(fishdexCmd.ExecuteScalar());
                if (fishdexTableExists == 0)
                {
                    string createFishdexTableQuery = @"
                        CREATE TABLE Fishdex (
                            UserID INT,
                            RecordID VARCHAR(100),
                            SmallestCaughtFish VARCHAR(100),
                            BiggestCaughtFish VARCHAR(100),
                            CaughtTimes INT,
                            CaughtInAreas VARCHAR(255),
                            CaughtBait VARCHAR(255),
                            NotepadNote TEXT,
                            PRIMARY KEY (UserID, RecordID),
                            FOREIGN KEY (UserID) REFERENCES Users(Id) ON DELETE CASCADE
                        );";
                    using (var createFishdexCmd = new MySqlCommand(createFishdexTableQuery, connection))
                    {
                        createFishdexCmd.ExecuteNonQuery();
                        Console.WriteLine("The 'Fishdex' table has been created.");
                    }
                }
            }

            // Check if required columns exist in the 'Fishdex' table
            string[] fishdexRequiredColumns = { "SmallestCaughtFish", "BiggestCaughtFish", "CaughtTimes", "CaughtInAreas", "CaughtBait", "NotepadNote" };
            foreach (var column in fishdexRequiredColumns)
            {
                string fishdexColumnCheckQuery = $@"
                    SELECT COUNT(*) FROM information_schema.columns
                    WHERE table_schema = DATABASE() AND table_name = 'Fishdex' AND column_name = '{column}'";
                using (var fishdexColumnCmd = new MySqlCommand(fishdexColumnCheckQuery, connection))
                {
                    int columnExists = Convert.ToInt32(fishdexColumnCmd.ExecuteScalar());
                    if (columnExists == 0)
                    {
                        string addColumnQuery = column switch
                        {
                            "SmallestCaughtFish" => "ALTER TABLE Fishdex ADD SmallestCaughtFish VARCHAR(100);",
                            "BiggestCaughtFish" => "ALTER TABLE Fishdex ADD BiggestCaughtFish VARCHAR(100);",
                            "CaughtTimes" => "ALTER TABLE Fishdex ADD CaughtTimes INT;",
                            "CaughtInAreas" => "ALTER TABLE Fishdex ADD CaughtInAreas VARCHAR(255);",
                            "CaughtBait" => "ALTER TABLE Fishdex ADD CaughtBait VARCHAR(255);",
                            "NotepadNote" => "ALTER TABLE Fishdex ADD NotepadNote TEXT;",
                            _ => throw new InvalidOperationException("Unknown column.")
                        };

                        using (var addCmd = new MySqlCommand(addColumnQuery, connection))
                        {
                            addCmd.ExecuteNonQuery();
                            Console.WriteLine($"Column '{column}' has been added to the 'Fishdex' table.");
                        }
                    }
                }
            }

            // --- Inventory Table Logic ---
            string inventoryTableCheckQuery = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'Inventory'";
            using (var inventoryCmd = new MySqlCommand(inventoryTableCheckQuery, connection))
            {
                int inventoryTableExists = Convert.ToInt32(inventoryCmd.ExecuteScalar());
                if (inventoryTableExists == 0)
                {
                    string createInventoryTableQuery = @"
                        CREATE TABLE Inventory (
                            UserID INT,
                            ItemID VARCHAR(100),
                            Quantity INT,
                            ItemType VARCHAR(50),
                            IsStackable BOOLEAN,
                            IsTradeable BOOLEAN,
                            PRIMARY KEY (UserID, ItemID),
                            FOREIGN KEY (UserID) REFERENCES Users(Id) ON DELETE CASCADE
                        );";
                    using (var createInventoryCmd = new MySqlCommand(createInventoryTableQuery, connection))
                    {
                        createInventoryCmd.ExecuteNonQuery();
                        Console.WriteLine("The 'Inventory' table has been created.");
                    }
                }
            }

            // Check if required columns exist in the 'Inventory' table
            string[] inventoryRequiredColumns = { "UserID", "ItemID", "Quantity", "ItemType", "IsStackable", "IsTradeable" };
            foreach (var column in inventoryRequiredColumns)
            {
                string inventoryColumnCheckQuery = $@"
                    SELECT COUNT(*) FROM information_schema.columns
                    WHERE table_schema = DATABASE() AND table_name = 'Inventory' AND column_name = '{column}'";
                using (var inventoryColumnCmd = new MySqlCommand(inventoryColumnCheckQuery, connection))
                {
                    int columnExists = Convert.ToInt32(inventoryColumnCmd.ExecuteScalar());
                    if (columnExists == 0)
                    {
                        string addColumnQuery = column switch
                        {
                            "UserID" => "ALTER TABLE Inventory ADD UserID INT;",
                            "ItemID" => "ALTER TABLE Inventory ADD ItemID VARCHAR(100);",
                            "Quantity" => "ALTER TABLE Inventory ADD Quantity INT;",
                            "ItemType" => "ALTER TABLE Inventory ADD ItemType VARCHAR(50);",
                            "IsStackable" => "ALTER TABLE Inventory ADD IsStackable BOOLEAN;",
                            "IsTradeable" => "ALTER TABLE Inventory ADD IsTradeable BOOLEAN;",
                            _ => throw new InvalidOperationException("Unknown column.")
                        };

                        using (var addCmd = new MySqlCommand(addColumnQuery, connection))
                        {
                            addCmd.ExecuteNonQuery();
                            Console.WriteLine($"Column '{column}' has been added to the 'Inventory' table.");
                        }
                    }
                }
            }

            // --- Messages Table Logic ---
            string messageTableCheckQuery = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'Messages'";
            using (var messageCmd = new MySqlCommand(messageTableCheckQuery, connection))
            {
                int messageTableExists = Convert.ToInt32(messageCmd.ExecuteScalar());
                if (messageTableExists == 0)
                {
                    string createMessageTableQuery = @"
                        CREATE TABLE Messages (
                            Id INT AUTO_INCREMENT PRIMARY KEY,
                            SenderID INT,
                            RecipientID INT,
                            DateSent DATETIME,
                            IsRead BOOLEAN,
                            FOREIGN KEY (SenderID) REFERENCES Users(Id) ON DELETE CASCADE,
                            FOREIGN KEY (RecipientID) REFERENCES Users(Id) ON DELETE CASCADE
                        );";
                    using (var createMessageCmd = new MySqlCommand(createMessageTableQuery, connection))
                    {
                        createMessageCmd.ExecuteNonQuery();
                        Console.WriteLine("The 'Messages' table has been created.");
                    }
                }
            }

            // Check if required columns exist in the 'Messages' table
            string[] messageRequiredColumns = { "SenderID", "RecipientID", "DateSent", "IsRead" };
            foreach (var column in messageRequiredColumns)
            {
                string messageColumnCheckQuery = $@"
                    SELECT COUNT(*) FROM information_schema.columns
                    WHERE table_schema = DATABASE() AND table_name = 'Messages' AND column_name = '{column}'";
                using (var messageColumnCmd = new MySqlCommand(messageColumnCheckQuery, connection))
                {
                    int columnExists = Convert.ToInt32(messageColumnCmd.ExecuteScalar());
                    if (columnExists == 0)
                    {
                        string addColumnQuery = column switch
                        {
                            "SenderID" => "ALTER TABLE Messages ADD SenderID INT;",
                            "RecipientID" => "ALTER TABLE Messages ADD RecipientID INT;",
                            "DateSent" => "ALTER TABLE Messages ADD DateSent DATETIME;",
                            "IsRead" => "ALTER TABLE Messages ADD IsRead BOOLEAN;",
                            _ => throw new InvalidOperationException("Unknown column.")
                        };

                        using (var addCmd = new MySqlCommand(addColumnQuery, connection))
                        {
                            addCmd.ExecuteNonQuery();
                            Console.WriteLine($"Column '{column}' has been added to the 'Messages' table.");
                        }
                    }
                }
            }
        }
    }
}
