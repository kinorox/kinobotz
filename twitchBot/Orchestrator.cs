using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace twitchBot
{
    public class Orchestrator : IHostedService, IOrchestrator
    {
        private readonly IRedisClient redisClient;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<Orchestrator> logger;
        private readonly Dictionary<Guid, BotConnection> connections = new();

        public Orchestrator(IRedisClient redisClient, IServiceProvider serviceProvider, ILogger<Orchestrator> logger)
        {
            this.redisClient = redisClient;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
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
            var botConnections = await GetConnections();

            foreach (var botConnection in botConnections.Values.Where(b => b.Active.HasValue && b.Active.Value))
            {
                try
                {
                    if (connections.ContainsKey(botConnection.Id)) continue;

                    var bot = (IBot)serviceProvider.GetService(typeof(IBot));
                    bot?.Connect(botConnection);

                    connections.Add(botConnection.Id, botConnection);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Couldn't connect to {botConnection.Id}.");
                }
            }
        }

        private async Task<IDictionary<string, BotConnection>> GetConnections()
        {
            var existingKeys = await redisClient.Db0.SearchKeysAsync("botconnection*");

            var botConnections = await redisClient.Db0.GetAllAsync<BotConnection>(new HashSet<string>(existingKeys));

            return botConnections;
        }
    }

    public interface IOrchestrator
    {
        Task RefreshConnections();
    }
}
