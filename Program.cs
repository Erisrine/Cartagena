using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.Extensions.Hosting;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Cartagena;
using Cartagena.Models;
using System.Reflection.Metadata;

class Program
{
    private const string CLIENT_ID = "cnfaczx3u66tev6siqdv7fm33224a0";

    static async Task Main(string[] args)
    {
        Console.Write("Enter your Twitch App's CLIENT SECRET\n");
        string clientSecret = ReadSecretFromConsole();
        var auth = new TwitchAuth(CLIENT_ID, clientSecret);

        await auth.RunAuthFlowAsync();

        Console.WriteLine($"Authenticated as: {auth.DisplayName}");
        RunTwitchBot(auth.UserLogin!, auth.AccessToken!, CLIENT_ID);
        await Task.Delay(-1);
    }

    static void RunTwitchBot(string username, string token, string clientId)
    {
        var creds = new ConnectionCredentials(username, token);

        var client = new TwitchClient();
        client.Initialize(creds, username);

        client.OnConnected += (s, e) =>
        {
            Console.WriteLine($"✅ Connected to Twitch as {username}");
        };

        client.OnMessageReceived += (s, e) =>
        {
            Console.WriteLine($"[{e.ChatMessage.Username}] {e.ChatMessage.Message}");

            if (e.ChatMessage.Message == "!hello")
            {
                client.SendMessage(e.ChatMessage.Channel, $"Hello, {e.ChatMessage.Username}!");
            }

            if (e.ChatMessage.Message == "!getlatest")
            {
                // Run the async API call without blocking the bot
                Task.Run(async () =>
                {
                    var latestUrl = await GetLastestVideoURL(e.ChatMessage.Channel, clientId, token);
                    if (!string.IsNullOrEmpty(latestUrl))
                        client.SendMessage(e.ChatMessage.Channel, $"Latest VOD: {latestUrl}");
                    else
                        client.SendMessage(e.ChatMessage.Channel, "Could not fetch the latest VOD.");
                });
            }
        };

        client.Connect();
    }

    private static string ReadSecretFromConsole()
    {
        string input = "";
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
                break;
            if (key.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input = input.Substring(0, input.Length - 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                input += key.KeyChar;
                Console.Write("*"); // show asterisk instead of the actual character
            }
        } while (true);

        Console.WriteLine();
        return input;
    }

    static async Task<string?> GetLastestVideoURL(string broadcasterUsername, string clientId, string accessToken)
    {
        using var http = new HttpClient();

        http.DefaultRequestHeaders.Add("Client-Id", clientId);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var userResponse = await http.GetAsync($"https://api.twitch.tv/helix/users?login={broadcasterUsername}");
        if (!userResponse.IsSuccessStatusCode) { return null; }

        using var userStream = await userResponse.Content.ReadAsStreamAsync();
        using var userJson = JsonDocument.Parse(userStream);
        var userId = userJson.RootElement.GetProperty("data")[0].GetProperty("id").GetString();

        var videoResponse = await http.GetAsync($"https://api.twitch.tv/helix/videos?user_id={userId}&type=archive&first=1");
        if (!videoResponse.IsSuccessStatusCode) { return null; }

        using var videoStream = await videoResponse.Content.ReadAsStreamAsync();
        using var videoJson = JsonDocument.Parse(videoStream);

        var videoUrl = videoJson.RootElement.GetProperty("data")[0].GetProperty("url").GetString();
        return videoUrl;

    }
}

