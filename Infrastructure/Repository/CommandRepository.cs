using Entities;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Infrastructure.Repository
{
    public class CommandRepository : ICommandRepository
    {
        private readonly IRedisClient _redisClient;

        public CommandRepository(IRedisClient redisClient)
        {
            _redisClient = redisClient;
        }

        public async Task<IDictionary<string, long>> GetExecutionCounters(Guid botConnectionId)
        {
            var keys = Commands.DefaultCommands.Select(c => $"{botConnectionId}:{c.Prefix}:counter");

            var counters = await _redisClient.Db0.GetAllAsync<long>(new HashSet<string>(keys));

            return counters;
        }

        public async Task<IDictionary<string, long>> GetExecutionCounters()
        {
            var botConnectionCounterKeys = await _redisClient.Db0.SearchKeysAsync("*:*:counter");

            var keys = Commands.DefaultCommands.Select(c => $"{c.Prefix}:counter").ToList();

            keys.AddRange(botConnectionCounterKeys);

            var counters = await _redisClient.Db0.GetAllAsync<long>(new HashSet<string>(keys));

            return counters;
        }

        public async Task<DateTime> GetLastExecutionTime(Guid botConnectionId, string commandPrefix, string username)
        {
            return await _redisClient.Db0.GetAsync<DateTime>($"{botConnectionId}:{commandPrefix}:lastexecution:{username}");
        }

        public async Task<DateTime> GetLastExecutionTime(Guid botConnectionId, string commandPrefix)
        {
            return await _redisClient.Db0.GetAsync<DateTime>($"{botConnectionId}:{commandPrefix}:lastexecution");
        }

        public async Task SetLastExecutionTime(Guid botConnectionId, string commandPrefix, string username, DateTime time)
        {
            await _redisClient.Db0.AddAsync($"{botConnectionId}:{commandPrefix}:lastexecution", time);
            await _redisClient.Db0.AddAsync($"{botConnectionId}:{commandPrefix}:lastexecution:{username}", time);
        }

        public void IncrementExecutionCounter(Guid botConnectionId, string commandPrefix)
        {
            _redisClient.Db0.Database.StringIncrement(new RedisKey($"{commandPrefix}:counter"));
            _redisClient.Db0.Database.StringIncrement(new RedisKey($"{botConnectionId}:{commandPrefix}:counter"));
        }
    }

    public interface ICommandRepository
    {
        Task<IDictionary<string, long>> GetExecutionCounters(Guid botConnectionId);
        Task<IDictionary<string, long>> GetExecutionCounters();
        Task<DateTime> GetLastExecutionTime(Guid botConnectionId, string commandPrefix, string username);
        Task<DateTime> GetLastExecutionTime(Guid botConnectionId, string commandPrefix);
        Task SetLastExecutionTime(Guid botConnectionId, string commandPrefix, string username, DateTime time);
        void IncrementExecutionCounter(Guid botConnectionId, string commandPrefix);
    }
}
