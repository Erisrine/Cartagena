using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cartagena.Google.Api
{
    public class SheetsWebhookClient
    {
        private readonly HttpClient _http;
        private readonly string _url;
        private readonly string _bearer;

        public SheetsWebhookClient(string webhookUrl, string bearerSecret = null)
        {
            _http = new HttpClient();
            _url = webhookUrl;
            _bearer = bearerSecret;
        }

        public async Task<bool> SendHighlightAsync(string username, string message)
        {
            var payload = new { username, message };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var req = new HttpRequestMessage(HttpMethod.Post, _url)
            {
                Content = content
            };

            if (!string.IsNullOrEmpty(_bearer))
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _bearer);

            var res = await _http.SendAsync(req);
            return res.IsSuccessStatusCode;
        }
    }
}
