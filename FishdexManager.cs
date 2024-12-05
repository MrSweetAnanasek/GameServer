using MySql.Data.MySqlClient;
using System.Net;

public class FishdexManager
{
    public static void HandleFishdex(dynamic requestData, HttpListenerResponse response, string connectionString)
    {
        int userId = requestData.userid;
        string action = requestData.action;

        using (var connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();

                switch (action.ToLower())
                {
                    case "add":
                        AddFishRecords(connection, userId, requestData, response);
                        break;
                    case "update":
                        UpdateFishRecords(connection, userId, requestData, response);
                        break;
                    case "get":
                        GetFishdex(connection, userId, response);
                        break;
                    default:
                        Program.SendResponse(response, new { status = "error", message = "Invalid action type." });
                        return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fishdex operation error: {ex.Message}");
                Program.SendResponse(response, new { status = "error", message = "Fishdex operation failed!" });
            }
        }
    }

    public static void AddFishRecords(MySqlConnection connection, int userId, dynamic requestData, HttpListenerResponse response)
    {
        var records = requestData.records.ToObject<List<dynamic>>();

        foreach (var record in records)
        {
            string recordId = record.id;
            string smallestCaughtFish = record.smallest_caught_fish;
            string biggestCaughtFish = record.biggest_caught_fish;
            int caughtTimes = record.caught_times;
            List<int> caughtInAreas = record.caught_in_areas.ToObject<List<int>>();
            List<string> caughtBait = record.caught_bait.ToObject<List<string>>();
            string notepadNote = record.notepad_note;

            // Check if the fish record already exists
            string checkExistenceQuery = "SELECT RecordID FROM Fishdex WHERE UserID = @UserID AND RecordID = @RecordID";
            using (var cmd = new MySqlCommand(checkExistenceQuery, connection))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@RecordID", recordId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Record exists, so we need to update it
                        UpdateFishRecords(connection, userId, record, response);
                        return;
                    }
                }
            }

            // If the record does not exist, insert the new record
            string insertQuery = @"
            INSERT INTO Fishdex (UserID, RecordID, SmallestCaughtFish, BiggestCaughtFish, CaughtTimes, CaughtInAreas, CaughtBait, NotepadNote)
            VALUES (@UserID, @RecordID, @SmallestCaughtFish, @BiggestCaughtFish, @CaughtTimes, @CaughtInAreas, @CaughtBait, @NotepadNote)";

            using (var cmd = new MySqlCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@RecordID", recordId);
                cmd.Parameters.AddWithValue("@SmallestCaughtFish", smallestCaughtFish);
                cmd.Parameters.AddWithValue("@BiggestCaughtFish", biggestCaughtFish);
                cmd.Parameters.AddWithValue("@CaughtTimes", caughtTimes);
                cmd.Parameters.AddWithValue("@CaughtInAreas", string.Join(",", caughtInAreas));
                cmd.Parameters.AddWithValue("@CaughtBait", string.Join(",", caughtBait));
                cmd.Parameters.AddWithValue("@NotepadNote", notepadNote);

                cmd.ExecuteNonQuery();
            }
        }

        Program.SendResponse(response, new { status = "success", message = "Fish records added successfully." });
    }

    public static void UpdateFishRecords(MySqlConnection connection, int userId, dynamic requestData, HttpListenerResponse response)
    {
        var records = requestData.records.ToObject<List<dynamic>>();

        foreach (var record in records)
        {
            string recordId = record.id;
            string smallestCaughtFish = record.smallest_caught_fish;
            string biggestCaughtFish = record.biggest_caught_fish;
            int caughtTimes = record.caught_times;
            List<int> caughtInAreas = record.caught_in_areas.ToObject<List<int>>();
            List<string> caughtBait = record.caught_bait.ToObject<List<string>>();
            string notepadNote = record.notepad_note;

            string selectQuery = "SELECT SmallestCaughtFish, BiggestCaughtFish, CaughtTimes FROM Fishdex WHERE UserID = @UserID AND RecordID = @RecordID";
            string existingSmallest = null, existingBiggest = null;
            int existingCaughtTimes = 0;

            using (var cmd = new MySqlCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@RecordID", recordId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        existingSmallest = reader["SmallestCaughtFish"].ToString();
                        existingBiggest = reader["BiggestCaughtFish"].ToString();
                        existingCaughtTimes = Convert.ToInt32(reader["CaughtTimes"]);
                    }
                    else
                    {
                        Program.SendResponse(response, new { status = "error", message = "Record not found." });
                        return;
                    }
                }
            }

            bool updateNeeded = false;
            string updatedSmallest = existingSmallest;
            string updatedBiggest = existingBiggest;

            // Compare and update smallest and biggest sizes
            if (string.Compare(smallestCaughtFish, existingSmallest) < 0)
            {
                updatedSmallest = smallestCaughtFish;
                updateNeeded = true;
            }

            if (string.Compare(biggestCaughtFish, existingBiggest) > 0)
            {
                updatedBiggest = biggestCaughtFish;
                updateNeeded = true;
            }

            // Always update the caught times
            int updatedCaughtTimes = existingCaughtTimes + caughtTimes;

            if (updateNeeded || caughtTimes > 0)
            {
                string updateQuery = @"
                UPDATE Fishdex
                SET SmallestCaughtFish = @SmallestCaughtFish, 
                    BiggestCaughtFish = @BiggestCaughtFish, 
                    CaughtTimes = @CaughtTimes, 
                    CaughtInAreas = @CaughtInAreas, 
                    CaughtBait = @CaughtBait, 
                    NotepadNote = @NotepadNote
                WHERE UserID = @UserID AND RecordID = @RecordID";

                using (var cmd = new MySqlCommand(updateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@RecordID", recordId);
                    cmd.Parameters.AddWithValue("@SmallestCaughtFish", updatedSmallest);
                    cmd.Parameters.AddWithValue("@BiggestCaughtFish", updatedBiggest);
                    cmd.Parameters.AddWithValue("@CaughtTimes", updatedCaughtTimes);
                    cmd.Parameters.AddWithValue("@CaughtInAreas", string.Join(",", caughtInAreas));
                    cmd.Parameters.AddWithValue("@CaughtBait", string.Join(",", caughtBait));
                    cmd.Parameters.AddWithValue("@NotepadNote", notepadNote);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        Program.SendResponse(response, new { status = "success", message = "Fish records updated successfully." });
    }

    public static void GetFishdex(MySqlConnection connection, int userId, HttpListenerResponse response)
    {
        string selectQuery = "SELECT RecordID, SmallestCaughtFish, BiggestCaughtFish, CaughtTimes, CaughtInAreas, CaughtBait, NotepadNote FROM Fishdex WHERE UserID = @UserID";

        using (var cmd = new MySqlCommand(selectQuery, connection))
        {
            cmd.Parameters.AddWithValue("@UserID", userId);

            using (var reader = cmd.ExecuteReader())
            {
                List<Dictionary<string, object>> fishdexList = new List<Dictionary<string, object>>();

                while (reader.Read())
                {
                    var fishdex = new Dictionary<string, object>
                {
                    { "RecordID", reader["RecordID"].ToString() },
                    { "SmallestCaughtFish", reader["SmallestCaughtFish"].ToString() },
                    { "BiggestCaughtFish", reader["BiggestCaughtFish"].ToString() },
                    { "CaughtTimes", Convert.ToInt32(reader["CaughtTimes"]) },
                    { "CaughtInAreas", reader["CaughtInAreas"].ToString().Split(',') },
                    { "CaughtBait", reader["CaughtBait"].ToString().Split(',') },
                    { "NotepadNote", reader["NotepadNote"].ToString() }
                };

                    fishdexList.Add(fishdex);
                }

                if (fishdexList.Count > 0)
                {
                    Program.SendResponse(response, new { status = "success", message = "Fishdex data retrieved successfully.", data = fishdexList });
                }
                else
                {
                    Program.SendResponse(response, new { status = "error", message = "No fish records found for this user." });
                }
            }
        }
    }
}
