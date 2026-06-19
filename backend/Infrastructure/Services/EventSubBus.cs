using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Infrastructure.Services
{
    /// <summary>
    /// Bridges EventSub notifications from the web tier (where the public callback lives)
    /// to the worker tier (where the bot acts), over a Redis pub/sub channel — both tiers
    /// already share Redis.
    /// </summary>
    public interface IEventSubBus
    {
        Task PublishAsync(string payload);
        Task SubscribeAsync(Func<string, Task> handler);
    }

    public class EventSubBus : IEventSubBus
    {
        private static readonly RedisChannel Channel = new("eventsub:notifications", RedisChannel.PatternMode.Literal);
        private readonly ISubscriber _subscriber;

        public EventSubBus(IRedisClient redisClient)
        {
            _subscriber = redisClient.Db0.Database.Multiplexer.GetSubscriber();
        }

        public Task PublishAsync(string payload) => _subscriber.PublishAsync(Channel, payload);

        public async Task SubscribeAsync(Func<string, Task> handler)
        {
            var queue = await _subscriber.SubscribeAsync(Channel);
            queue.OnMessage(async message =>
            {
                if (message.Message.HasValue)
                {
                    await handler(message.Message.ToString());
                }
            });
        }
    }
}
