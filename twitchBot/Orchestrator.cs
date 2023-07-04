using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace twitchBot
{
    public class Orchestrator : IHostedService
    {
        private readonly IRedisClient redisClient;
        private readonly IServiceProvider serviceProvider;

        public Orchestrator(IRedisClient redisClient, IServiceProvider serviceProvider)
        {
            this.redisClient = redisClient;
            this.serviceProvider = serviceProvider;

        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var existingKeys = await redisClient.Db0.SearchKeysAsync("botconnection*");

            var botConnections = await redisClient.Db0.GetAllAsync<BotConnection>(new HashSet<string>(existingKeys));

            foreach (var botConnection in botConnections)
            {
                var bot = (IBot)serviceProvider.GetService(typeof(IBot));
                bot?.Connect(botConnection.Value);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
