using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

class Program
{
    static void Main(string[] args)
    {
        string prefix = "http://localhost:8080/";
        string connectionString = "Server=localhost;Database=game;User=root;";

        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(prefix);

        try
        {
            DatabaseManager.EnsureTableAndColumnsExist(connectionString);

            listener.Start();
            Console.WriteLine($"Game Server running on: {prefix}");
            Console.WriteLine("Waiting for requests...");

            while (true)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    ProcessRequest(context, connectionString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing request: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server Error: {ex.Message}");
        }
        finally
        {
            listener.Stop();
            Console.WriteLine("Game Server stopped.");
        }
    }

    private static void ProcessRequest(HttpListenerContext context, string connectionString)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        try
        {
            if (request.HttpMethod == "POST" && request.RawUrl == "/backend")
            {
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    string jsonData = reader.ReadToEnd();
                    var requestData = JsonConvert.DeserializeObject<dynamic>(jsonData);
                    string type = requestData.type;

                    switch (type.ToLower())
                    {
                        case "register":
                            AuthManager.HandleRegister(requestData, response, connectionString);
                            break;
                        case "login":
                            AuthManager.HandleLogin(requestData, response, connectionString);
                            break;
                        case "inventory":
                            InventoryManager.HandleInventory(requestData, response, connectionString);
                            break;
                        case "message":
                            MessagesManager.HandleMessages(requestData, response, connectionString);
                            break;
                        case "fishdex":
                            FishdexManager.HandleFishdex(requestData, response, connectionString);
                            break;
                        default:
                            throw new ArgumentException("Invalid request type.");
                    }
                }
            }
            else
            {
                response.StatusCode = 404;
                SendResponse(response, new { status = "error", message = "Endpoint not found" });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing request: {ex.Message}");
            response.StatusCode = 500;
            SendResponse(response, new { status = "error", message = "Internal server error" });
        }
    }
	
    private static bool responseSent = false;

    public static void SendResponse(HttpListenerResponse response, object responseObject)
    {
        try
        {
            if (!responseSent)
            {
                string responseJson = JsonConvert.SerializeObject(responseObject);
                byte[] buffer = Encoding.UTF8.GetBytes(responseJson);

                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.StatusCode = 200;

                using (var output = response.OutputStream)
                {
                    output.Write(buffer, 0, buffer.Length);
                }

                responseSent = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending response: {ex.Message}");
            Console.WriteLine("Response details:");
            Console.WriteLine($"ContentType: {response.ContentType}");
            Console.WriteLine($"ContentLength64: {response.ContentLength64}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");

            try
            {
                string responseJson = JsonConvert.SerializeObject(responseObject);
                Console.WriteLine($"Response Object: {responseJson}");
            }
            catch (Exception jsonEx)
            {
                Console.WriteLine($"Error serializing response object: {jsonEx.Message}");
            }
        }
    }
}
