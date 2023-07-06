using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace twitchBot
{
    public class Orchestrator : IHostedService
    {
        private readonly IRedisClient redisClient;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<Orchestrator> logger;

        public Orchestrator(IRedisClient redisClient, IServiceProvider serviceProvider, ILogger<Orchestrator> logger)
        {
            this.redisClient = redisClient;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var existingKeys = await redisClient.Db0.SearchKeysAsync("botconnection*");

            var botConnections = await redisClient.Db0.GetAllAsync<BotConnection>(new HashSet<string>(existingKeys));

            foreach (var botConnection in botConnections.Values.Where(b => b.Active))
            {
                try
                {
                    var bot = (IBot)serviceProvider.GetService(typeof(IBot));
                    bot?.Connect(botConnection);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Couldn't connect to {botConnection.Id}.");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
