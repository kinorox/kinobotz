using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// Creates the Twitch EventSub webhook subscriptions for a channel (idempotently),
    /// using an app access token. Replaces the per-channel PubSub ListenTo* calls.
    /// </summary>
    public interface IEventSubSubscriptionService
    {
        Task EnsureSubscriptionsAsync(string broadcasterId);
    }

    public class EventSubSubscriptionService : IEventSubSubscriptionService
    {
        private static readonly (string Type, string Version)[] DesiredTypes =
        {
            ("stream.online", "1"),
            ("stream.offline", "1"),
            ("channel.cheer", "1"),
            ("channel.subscription.message", "1"),
        };

        private const string DefaultCallback = "https://kinobotz.herokuapp.com/eventsub/callback";
        private const string SubscriptionsUrl = "https://api.twitch.tv/helix/eventsub/subscriptions";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventSubSubscriptionService> _logger;

        public EventSubSubscriptionService(HttpClient httpClient, IConfiguration configuration, ILogger<EventSubSubscriptionService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task EnsureSubscriptionsAsync(string broadcasterId)
        {
            if (string.IsNullOrEmpty(broadcasterId)) return;

            var clientId = _configuration["client_id"];
            var clientSecret = _configuration["client_secret"];
            var secret = _configuration["eventsub_secret"];
            var callback = _configuration["eventsub_callback_url"];
            if (string.IsNullOrEmpty(callback)) callback = DefaultCallback;

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(secret))
            {
                _logger.LogWarning("EventSub subscriptions skipped for {Broadcaster}: client_id/client_secret/eventsub_secret not configured.", broadcasterId);
                return;
            }

            try
            {
                var appToken = await GetAppTokenAsync(clientId, clientSecret);
                if (appToken == null) return;

                var existing = await GetExistingTypesAsync(clientId, appToken, broadcasterId);

                foreach (var (type, version) in DesiredTypes)
                {
                    if (existing.Contains(type)) continue;
                    await CreateAsync(clientId, appToken, type, version, broadcasterId, callback, secret);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error ensuring EventSub subscriptions for {Broadcaster}", broadcasterId);
            }
        }

        private async Task<string?> GetAppTokenAsync(string clientId, string clientSecret)
        {
            var url = $"https://id.twitch.tv/oauth2/token?client_id={clientId}&client_secret={clientSecret}&grant_type=client_credentials";
            using var resp = await _httpClient.PostAsync(url, null);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("EventSub: failed to get app token ({Status})", (int)resp.StatusCode);
                return null;
            }
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("access_token").GetString();
        }

        private async Task<HashSet<string>> GetExistingTypesAsync(string clientId, string appToken, string broadcasterId)
        {
            var result = new HashSet<string>();
            using var req = new HttpRequestMessage(HttpMethod.Get, SubscriptionsUrl);
            req.Headers.Add("Client-Id", clientId);
            req.Headers.Add("Authorization", $"Bearer {appToken}");
            using var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return result;

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            foreach (var sub in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                var cond = sub.GetProperty("condition");
                if (!cond.TryGetProperty("broadcaster_user_id", out var bid) || bid.GetString() != broadcasterId) continue;

                var status = sub.GetProperty("status").GetString();
                if (status is "enabled" or "webhook_callback_verification_pending")
                {
                    var type = sub.GetProperty("type").GetString();
                    if (type != null) result.Add(type);
                }
            }
            return result;
        }

        private async Task CreateAsync(string clientId, string appToken, string type, string version, string broadcasterId, string callback, string secret)
        {
            var payload = new
            {
                type,
                version,
                condition = new { broadcaster_user_id = broadcasterId },
                transport = new { method = "webhook", callback, secret }
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, SubscriptionsUrl) { Content = JsonContent.Create(payload) };
            req.Headers.Add("Client-Id", clientId);
            req.Headers.Add("Authorization", $"Bearer {appToken}");

            using var resp = await _httpClient.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                _logger.LogInformation("EventSub subscription created: {Type} for {Broadcaster}", type, broadcasterId);
            }
            else
            {
                var err = await resp.Content.ReadAsStringAsync();
                _logger.LogWarning("EventSub create {Type} for {Broadcaster} failed ({Status}): {Error}", type, broadcasterId, (int)resp.StatusCode, err);
            }
        }
    }
}
