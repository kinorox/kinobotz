using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace twitchBot.Jobs
{
    [DisallowConcurrentExecution]
    public class RefreshConnectionsJob : IJob
    {
        private readonly Orchestrator _orchestrator;
        private readonly ILogger<RefreshConnectionsJob> _logger;

        public RefreshConnectionsJob(Orchestrator orchestrator, ILogger<RefreshConnectionsJob> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Refreshing connections...");

            await _orchestrator.RefreshConnections();

            _logger.LogInformation("Connections refreshed.");
        }
    }
}
