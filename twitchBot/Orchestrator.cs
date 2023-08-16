using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace twitchBot
{
    public class Orchestrator : IHostedService, IOrchestrator
    {
        private readonly IBotConnectionRepository _botConnectionRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Orchestrator> _logger;
        public readonly Dictionary<Guid, BotConnection> Connections = new();

        public Orchestrator(IServiceProvider serviceProvider, ILogger<Orchestrator> logger, IBotConnectionRepository botConnectionRepository)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _botConnectionRepository = botConnectionRepository;
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

        private async Task Connect()
        {
            var botConnections = await _botConnectionRepository.GetAll();

            //TODO remove - only for testing
            //botConnections = botConnections.Where(b => b.Login == "kinobotz").ToList();

            foreach (var botConnection in botConnections.Where(b => b.Active.HasValue && b.Active.Value))
            {
                try
                {
                    if (Connections.ContainsKey(botConnection.Id)) continue;

                    var bot = (IBot)_serviceProvider.GetService(typeof(IBot));
                    bot?.Connect(botConnection);

                    Connections.Add(botConnection.Id, botConnection);
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
