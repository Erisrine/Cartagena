
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Cartagena
{
    public class TwitchAuth
    {
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string redirectUri = "http://localhost:5000/callback";

        private static readonly HttpClient http = new HttpClient();
        public TwitchBot? BotReference { get; set; }

        public string ClientId => clientId;
        public string? AccessToken { get; private set; }
        public string? RefreshToken { get; private set; }
        public string? UserId { get; private set; }
        public string? UserLogin { get; private set; }
        public string? DisplayName { get; private set; }
        public DateTime ExpiresAt { get; private set; } = DateTime.MinValue;

        public TwitchAuth(string clientId, string clientSecret)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
        }

        private void SetTokenInfo(JsonDocument doc)
        {
            AccessToken = doc.RootElement.GetProperty("access_token").GetString();
            RefreshToken = doc.RootElement.GetProperty("refresh_token").GetString();
            int expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 600); // Refresh 10 minutes early
        }

        public async Task RunAuthFlowAsync()
        {
            string state = Guid.NewGuid().ToString("N");
            string authUrl = $"https://id.twitch.tv/oauth2/authorize" +
                             $"?client_id={clientId}" +
                             $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                             $"&response_type=code" +
                             $"&scope=chat:read+chat:edit+user:read:email" +
                             $"&state={state}";

            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            using var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5000/callback/");
            listener.Start();
            Console.WriteLine("Waiting for Twitch login...");

            var context = await listener.GetContextAsync();
            var request = context.Request;
            var code = request.QueryString["code"];
            var returnedState = request.QueryString["state"];

            var response = context.Response;
            string responseString = "<html><body>You may now close this window.</body></html>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();

            listener.Stop();

            if (returnedState != state)
                throw new Exception("State mismatch! Possible CSRF attack.");

            await ExchangeCodeForTokenAsync(code!);
            await FetchUserInfoAsync();
        }

        private async Task ExchangeCodeForTokenAsync(string code)
        {
            var tokenResponse = await http.PostAsync("https://id.twitch.tv/oauth2/token",
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("client_id", clientId),
                    new KeyValuePair<string,string>("client_secret", clientSecret),
                    new KeyValuePair<string,string>("code", code),
                    new KeyValuePair<string,string>("grant_type", "authorization_code"),
                    new KeyValuePair<string,string>("redirect_uri", redirectUri)
                }));

            tokenResponse.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
            SetTokenInfo(doc);
            await FetchUserInfoAsync();

            BotReference?.ApplyNewToken(AccessToken!);
        }

        private async Task FetchUserInfoAsync()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/users");
            req.Headers.Add("Authorization", $"Bearer {AccessToken}");
            req.Headers.Add("Client-Id", clientId);

            var userResponse = await http.SendAsync(req);
            userResponse.EnsureSuccessStatusCode();
            using var userDoc = JsonDocument.Parse(await userResponse.Content.ReadAsStringAsync());
            var user = userDoc.RootElement.GetProperty("data")[0];

            UserId = user.GetProperty("id").GetString();
            UserLogin = user.GetProperty("login").GetString();
            DisplayName = user.GetProperty("display_name").GetString();
        }

        public async Task<bool> ValidateAsync()
        {
            if (string.IsNullOrEmpty(AccessToken))
                return false;

            var req = new HttpRequestMessage(HttpMethod.Get, "https://id.twitch.tv/oauth2/validate");
            req.Headers.Add("Authorization", $"OAuth {AccessToken}");

            var response = await http.SendAsync(req);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RefreshAccessTokenAsync()
        {
            if (string.IsNullOrEmpty(RefreshToken))
                return false;

            var response = await http.PostAsync("https://id.twitch.tv/oauth2/token",
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("client_id", clientId),
                    new KeyValuePair<string,string>("client_secret", clientSecret),
                    new KeyValuePair<string,string>("refresh_token", RefreshToken),
                    new KeyValuePair<string,string>("grant_type", "refresh_token")
                }));

            if (!response.IsSuccessStatusCode)
                return false;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            SetTokenInfo(doc);

            // Refresh user info after token rotation
            await FetchUserInfoAsync();
            Console.WriteLine("Token refreshed successfully");
            BotReference?.ApplyNewToken(AccessToken!);

            return true;
        }

        public async Task EnsureValidAsync()
        {
            if (string.IsNullOrEmpty(AccessToken))
            {
                await RunAuthFlowAsync();
                return;
            }

            if (DateTime.UtcNow >= ExpiresAt || !await ValidateAsync())
            {
                Console.WriteLine("Token expired or invalid, refreshing...");
                if (!await RefreshAccessTokenAsync())
                {
                    Console.WriteLine("Refresh failed, starting full auth flow...");
                    await RunAuthFlowAsync();
                }
            }

            await FetchUserInfoAsync();
        }

        private CancellationTokenSource? _cts;
        public async Task StartTokenWatcherAsync()
        {
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Only refresh if we're within 10 minutes of expiry
                    var now = DateTime.UtcNow;
                    var timeUntilExpiry = ExpiresAt - now;

                    if (string.IsNullOrEmpty(AccessToken) || timeUntilExpiry <= TimeSpan.Zero)
                    {
                        Console.WriteLine("Token expired or missing, refreshing...");

                        bool refreshed = await RefreshAccessTokenAsync();
                        if (!refreshed)
                        {
                            Console.WriteLine("Refresh failed, starting full auth flow...");
                            await RunAuthFlowAsync();
                        }

                        await FetchUserInfoAsync(); // Only fetch user info after token changes
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in token watcher: {ex.Message}");
                }

                // Sleep until the token is close to expiry or 1 minute, whichever is smaller
                double delay = Math.Min((ExpiresAt - DateTime.UtcNow - TimeSpan.FromMinutes(1)).TotalMilliseconds,
                                    TimeSpan.FromMinutes(1).TotalMilliseconds);

                if (delay < 0) delay = TimeSpan.FromSeconds(10).TotalMilliseconds; // short retry if expired

                await Task.Delay((int)delay);   //this should never overflow the Int32 limit anyway...
                                                //if this function decides to wait over 68 years, there are other issues to worry about
            }
        }

        public void StopTokenWatcher()
        {
            _cts?.Cancel();
            _cts = null;
        }
    }
}
