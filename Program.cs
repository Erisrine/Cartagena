using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using TwitchLib.Client;
using TwitchLib.Client.Models;

class Program
{
    static async Task Main(string[] args)
    {
        _ = Task.Run(() => RunTwitchBot());
        
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/", () => "Bot is running! Open Twitch chat to interact.");
        app.MapGet("/status", () => new { running = true });

        await app.RunAsync(); // Keeps the process alive
    }

    static void RunTwitchBot()
    {
        
        var username = "YourTwitchUsername";
        var token = "oauth:your_token_here";

        var creds = new ConnectionCredentials(username, token);

        var client = new TwitchClient();
        client.Initialize(creds, "YourChannelName");

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
        };

        client.Connect();
    }
}
