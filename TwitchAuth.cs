using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cartagena
{
    public class TwitchAuth
    {
        private readonly string? clientId;
        private readonly string? clientSecret;
        private readonly string redirectUri = "http://localhost:5000/callback"; // must match Twitch app settings

        public string? AccessToken { get; private set; }
        public string? RefreshToken { get; private set; }
        public string? UserId { get; private set; }
        public string? UserLogin { get; private set; }
        public string? DisplayName { get; private set; }

        public TwitchAuth(string clientId, string clientSecret)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
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

            // Step 1: Open browser
            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            // Step 2: Wait for redirect on localhost
            using var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5000/callback/");
            listener.Start();
            Console.WriteLine("Waiting for Twitch login...");

            var context = await listener.GetContextAsync();
            var request = context.Request;

            var code = request.QueryString["code"];
            var returnedState = request.QueryString["state"];

            // Respond to browser
            var response = context.Response;
            string responseString = "<html><body>You may now close this window.</body></html>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();

            listener.Stop();

            if (returnedState != state)
                throw new Exception("State mismatch! Possible CSRF attack.");

            // Step 3: Exchange code for token
            using var http = new HttpClient();
            var tokenResponse = await http.PostAsync("https://id.twitch.tv/oauth2/token",
                new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string,string>("client_id", clientId!),
                new KeyValuePair<string,string>("client_secret", clientSecret!),
                new KeyValuePair<string,string>("code", code!),
                new KeyValuePair<string,string>("grant_type", "authorization_code"),
                new KeyValuePair<string,string>("redirect_uri", redirectUri)
                }));

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(tokenJson);

            AccessToken = doc.RootElement.GetProperty("access_token").GetString();
            RefreshToken = doc.RootElement.GetProperty("refresh_token").GetString();

            // Step 4: Get user info
            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/users");
            req.Headers.Add("Authorization", $"Bearer {AccessToken}");
            req.Headers.Add("Client-Id", clientId);

            var userResponse = await http.SendAsync(req);
            var userJson = await userResponse.Content.ReadAsStringAsync();

            using var userDoc = JsonDocument.Parse(userJson);
            var user = userDoc.RootElement.GetProperty("data")[0];
#pragma warning disable CS8601 // Possible null reference assignment.
            UserId = user.GetProperty("id").GetString();
#pragma warning restore CS8601 // Possible null reference assignment.
            UserLogin = user.GetProperty("login").GetString();
            DisplayName = user.GetProperty("display_name").GetString();
        }
    }
}
