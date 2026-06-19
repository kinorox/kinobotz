using System.Text;
using System.Text.Json;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace webapi.Controllers
{
    // Public Twitch EventSub webhook callback. Verifies the signature, answers the
    // one-time verification challenge, dedupes redeliveries, and relays notifications
    // to the worker over the Redis bus (where the bot acts).
    [ApiController]
    public class EventSubController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IEventSubBus _bus;
        private readonly IRedisClient _redisClient;
        private readonly ILogger<EventSubController> _logger;

        public EventSubController(IConfiguration configuration, IEventSubBus bus, IRedisClient redisClient, ILogger<EventSubController> logger)
        {
            _configuration = configuration;
            _bus = bus;
            _redisClient = redisClient;
            _logger = logger;
        }

        [HttpPost("~/eventsub/callback")]
        public async Task<IActionResult> Callback()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            var messageId = Request.Headers["Twitch-Eventsub-Message-Id"].ToString();
            var timestamp = Request.Headers["Twitch-Eventsub-Message-Timestamp"].ToString();
            var signature = Request.Headers["Twitch-Eventsub-Message-Signature"].ToString();
            var messageType = Request.Headers["Twitch-Eventsub-Message-Type"].ToString();

            if (!EventSubSignature.IsValid(_configuration["eventsub_secret"], messageId, timestamp, body, signature))
            {
                _logger.LogWarning("EventSub callback: invalid signature (message {MessageId})", messageId);
                return Unauthorized();
            }

            switch (messageType)
            {
                case "webhook_callback_verification":
                    var challenge = JsonDocument.Parse(body).RootElement.GetProperty("challenge").GetString();
                    return Content(challenge ?? string.Empty, "text/plain");

                case "revocation":
                    _logger.LogWarning("EventSub subscription revoked: {Body}", body);
                    return Ok();

                case "notification":
                    var seenKey = $"eventsub:msg:{messageId}";
                    if (await _redisClient.Db0.ExistsAsync(seenKey))
                    {
                        return Ok();
                    }
                    await _redisClient.Db0.AddAsync(seenKey, true, TimeSpan.FromMinutes(10));
                    await _bus.PublishAsync(body);
                    return Ok();

                default:
                    return Ok();
            }
        }
    }
}
