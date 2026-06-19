using System.Threading;
using System.Threading.Tasks;
using Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace twitchBot.Jobs
{
    // Phase 4 step 1 (observe mode): logs EventSub notifications relayed from the web tier,
    // so we can confirm end-to-end webhook delivery while PubSub still handles the real
    // events. Step 2 routes these into the command pipeline and retires PubSub.
    public class EventSubObserver : IHostedService
    {
        private readonly IEventSubBus _bus;
        private readonly ILogger<EventSubObserver> _logger;

        public EventSubObserver(IEventSubBus bus, ILogger<EventSubObserver> logger)
        {
            _bus = bus;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _bus.SubscribeAsync(payload =>
            {
                _logger.LogInformation("EventSub notification received (observe mode): {Payload}", payload);
                return Task.CompletedTask;
            });
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
