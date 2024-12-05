using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Data;

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
                HttpListenerContext context = listener.GetContext();
                ProcessRequest(context, connectionString);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server Error: {ex}");
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

    

    public static void SendResponse(HttpListenerResponse response, object responseObject)
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
    }
}
