using Cartagena.Twitch.API;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace Cartagena
{
    public class TwitchBot
    {
        private readonly TwitchClient client;
        private readonly TwitchAuth auth;
        private TwitchApi? api;

        public TwitchBot(string CLIENT_ID, string CLIENT_SECRET)
        {
            auth = new TwitchAuth(CLIENT_ID, CLIENT_SECRET);
            client = new TwitchClient();
            RunCommands();
        }

        public async Task<bool> InitializeAsync()
        {
            auth.BotReference = this;

            // Run the auth flow if AccessToken is missing or invalid
            if (string.IsNullOrEmpty(auth.AccessToken) || !await auth.ValidateAsync())
            {
                Console.WriteLine("Starting full authentication flow...");
                await auth.RunAuthFlowAsync();
            }
            if (!await auth.ValidateAsync())
            {
                Console.WriteLine("Authenticaion Not Valid");
                return false;
            }
            if (string.IsNullOrEmpty(auth.UserLogin) || string.IsNullOrEmpty(auth.AccessToken))
            {
                Console.WriteLine("Auth did not produce valid credentials.");
                return false;
            }
            // After auth, create the API client
            api = new TwitchApi(auth.ClientId, auth.AccessToken!);

            // Start the background token watcher
            _ = auth.StartTokenWatcherAsync();

            client.Initialize(new ConnectionCredentials(auth.UserLogin, auth.AccessToken), auth.UserLogin);
            client.OnConnected += (s, e) =>
            {
                Console.WriteLine($"Connected to Twitch as {auth.UserLogin}");
            };

            Console.WriteLine($"Authenticated INIT as {auth.DisplayName}");
            return true;
        }

        public void ApplyNewToken(string newAccessToken)
        {
            Console.WriteLine("Access token updated in TwitchBot");
            api?.UpdateAccessToken(newAccessToken);

            if (client.IsConnected)
            {
                Console.WriteLine("Refreshing Twitch client credentials...");
                client.Disconnect();
                client.SetConnectionCredentials(new ConnectionCredentials(auth.UserLogin, newAccessToken));
                Console.WriteLine($"Reconnected to Twitch with new token as {auth.UserLogin}");
            }
            else
            {
                client.Initialize(new ConnectionCredentials(auth.UserLogin, newAccessToken), auth.UserLogin);
                client.Connect();
            }
        }

        public bool ClientIsConnected()
        {
            return client.IsConnected;
        }

        private EventHandler<OnMessageReceivedArgs>? _chatHandler; //TwitchLib.Client.Events.OnMessageReceivedArgs
        public void RunCommands()
        {
            if (_chatHandler != null)
            {
                client.OnMessageReceived -= _chatHandler; //this prevents multiple subscriptions, which just makes commands fire multiple times.
            }

            _chatHandler = (s, e) =>
            {
                Console.WriteLine($"[{e.ChatMessage.Username}] {e.ChatMessage.Message}");

                if (e.ChatMessage.Message == "!cartagena")
                {
                    client.SendMessage(e.ChatMessage.Channel, $"Hello, {e.ChatMessage.Username}!");
                }

                if (e.ChatMessage.Message == "!highlight")
                {

                }
                ;
            };

            client.OnMessageReceived += _chatHandler;
        }

        public void Shutdown() 
        {
            Console.WriteLine("Shutting down TwitchBot...");

            auth.StopTokenWatcher();

            client.OnConnected -= (s, e) =>
            {
                Console.WriteLine($"Connected to Twitch as {auth.UserLogin}");
            };

            client.OnMessageReceived -= _chatHandler;

            if (client.IsConnected)
            {
                client.Disconnect();
                Console.WriteLine("Twitch client disconnected.");
            }

            if (api is IDisposable disposableApi)
            {
                disposableApi.Dispose();
                Console.WriteLine("Twitch API client disposed.");
            }

            auth.BotReference = null;

            Console.WriteLine("TwitchBot shutdown complete.");
        }

    }
}