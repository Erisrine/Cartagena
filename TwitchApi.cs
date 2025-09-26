namespace Cartagena.Twitch.API
{
    public class TwitchApi
    {
        private readonly HttpClient http;
        public TwitchApi(string clientId, string accessToken)
        {
            http = new HttpClient();
            http.DefaultRequestHeaders.Add("Client-Id", clientId);
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        }

        public void UpdateAccessToken(string newToken)
        {
            http.DefaultRequestHeaders.Remove("Authorization");
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {newToken}");
        }

        public async Task<string?> GetLatestVodURL(string userId)
        {
            await Task.Delay(1);
            return "";
        }

        public async Task<TimeSpan?> GetStreamUptime(string userId)
        {
            await Task.Delay(1);
            return new TimeSpan();
        }
    }
}