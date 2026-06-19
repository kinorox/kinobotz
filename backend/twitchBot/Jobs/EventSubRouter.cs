using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace twitchBot.Jobs
{
    // Subscribes to the EventSub Redis bus and routes each notification to the bot
    // instance for that broadcaster (replaces the former Twitch PubSub event handlers).
    public class EventSubRouter : IHostedService
    {
        private readonly IEventSubBus _bus;
        private readonly Orchestrator _orchestrator;
        private readonly ILogger<EventSubRouter> _logger;

        public EventSubRouter(IEventSubBus bus, Orchestrator orchestrator, ILogger<EventSubRouter> logger)
        {
            _bus = bus;
            _orchestrator = orchestrator;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _bus.SubscribeAsync(HandleAsync);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task HandleAsync(string payload)
        {
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                var type = root.GetProperty("subscription").GetProperty("type").GetString();
                var ev = root.GetProperty("event");
                var broadcasterId = ev.GetProperty("broadcaster_user_id").GetString();

                if (string.IsNullOrEmpty(broadcasterId)) return;

                var bot = _orchestrator.GetBot(broadcasterId);
                if (bot == null)
                {
                    _logger.LogWarning("EventSub: no connected bot for broadcaster {Broadcaster} (type {Type})", broadcasterId, type);
                    return;
                }

                _logger.LogInformation("EventSub routing {Type} for {Broadcaster}", type, broadcasterId);

                switch (type)
                {
                    case "stream.online":
                        await bot.HandleStreamUpAsync();
                        break;
                    case "stream.offline":
                        await bot.HandleStreamDownAsync();
                        break;
                    case "channel.cheer":
                        var bits = ev.TryGetProperty("bits", out var b) ? b.GetInt32() : 0;
                        var cheerMessage = ev.TryGetProperty("message", out var m) ? m.GetString() : string.Empty;
                        await bot.HandleBitsAsync(bits, cheerMessage ?? string.Empty);
                        break;
                    case "channel.subscription.message":
                        var months = ev.TryGetProperty("cumulative_months", out var cm) ? cm.GetInt32() : 0;
                        var subText = string.Empty;
                        if (ev.TryGetProperty("message", out var sm) && sm.ValueKind == JsonValueKind.Object
                            && sm.TryGetProperty("text", out var t))
                        {
                            subText = t.GetString() ?? string.Empty;
                        }
                        await bot.HandleSubscriptionAsync(months, subText);
                        break;
                    default:
                        _logger.LogInformation("EventSub: unhandled type {Type}", type);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "EventSub routing error");
            }
        }
    }
}
