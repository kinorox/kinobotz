using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace twitchBot
{
    public class Orchestrator : IHostedService, IOrchestrator
    {
        private readonly IBotConnectionRepository _botConnectionRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventSubSubscriptionService _eventSubSubscriptionService;
        private readonly ILogger<Orchestrator> _logger;
        public readonly Dictionary<Guid, BotConnection> Connections = new();
        private readonly Dictionary<string, IBot> _botsByChannelId = new();

        public Orchestrator(IServiceProvider serviceProvider, ILogger<Orchestrator> logger, IBotConnectionRepository botConnectionRepository, IEventSubSubscriptionService eventSubSubscriptionService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _botConnectionRepository = botConnectionRepository;
            _eventSubSubscriptionService = eventSubSubscriptionService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Connect();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task RefreshConnections()
        {
            await Connect();
        }

        /// <summary>Returns the connected bot for a broadcaster id (Twitch channel id), or null.</summary>
        public IBot GetBot(string channelId)
        {
            return channelId != null && _botsByChannelId.TryGetValue(channelId, out var bot) ? bot : null;
        }

        private async Task Connect()
        {
            var botConnections = await _botConnectionRepository.GetAll();

            foreach (var botConnection in botConnections.Where(b => b.Active.HasValue && b.Active.Value))
            {
                try
                {
                    if (Connections.ContainsKey(botConnection.Id)) continue;

                    var bot = (IBot)_serviceProvider.GetService(typeof(IBot));
                    bot?.Connect(botConnection);

                    Connections.Add(botConnection.Id, botConnection);

                    // register for EventSub routing + ensure the channel's webhook subscriptions exist
                    if (bot != null && !string.IsNullOrEmpty(botConnection.ChannelId))
                    {
                        _botsByChannelId[botConnection.ChannelId] = bot;
                        await _eventSubSubscriptionService.EnsureSubscriptionsAsync(botConnection.ChannelId);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Couldn't connect to {botConnection.Id}.");
                }
            }
        }
    }

    public interface IOrchestrator
    {
        Task RefreshConnections();
    }
}
