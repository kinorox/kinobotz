using Entities;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using Command = Entities.Command;

namespace Infrastructure.Repository
{
    public class BotConnectionRepository : IBotConnectionRepository
    {
        private readonly IRedisClient _redisClient;

        public BotConnectionRepository(IRedisClient redisClient)
        {
            _redisClient = redisClient;
        }

        public async Task<BotConnection?> Get(string id, string channelId, string login)
        {
            return await _redisClient.Db0.GetAsync<BotConnection>($"botconnection:{id}:{channelId}:{login}");
        }

        public async Task<ICollection<BotConnection>> GetAll(bool grabCommands = false)
        {
            var existingKeys = await _redisClient.Db0.SearchKeysAsync("botconnection:*:*:*");

            var botConnections = await _redisClient.Db0.GetAllAsync<BotConnection>(new HashSet<string>(existingKeys));
            if (!grabCommands) return botConnections.Values;

            foreach (var botConnection in botConnections)
            {
                if (botConnection.Value != null)
                    botConnection.Value.ChannelCommands = await GetCommands(botConnection.Value.Id);
            }
            return botConnections.Values;
        }

        private async Task<BotConnection> GetBotConnection(string searchPattern, bool grabCommands)
        {
            var existingKeys = await _redisClient.Db0.SearchKeysAsync(searchPattern);

            if (!existingKeys.Any())
                return null;

            var result = await _redisClient.Db0.GetAsync<BotConnection>(existingKeys.FirstOrDefault());

            if (result != null && grabCommands) result.ChannelCommands = await GetCommands(result.Id);

            return result;
        }

        public async Task<BotConnection?> GetById(string id, bool grabCommands)
        {
            return await GetBotConnection($"botconnection:{id}:*:*", grabCommands);
        }

        public async Task<BotConnection?> GetByChannelId(string channelId, bool grabCommands)
        {
            return await GetBotConnection($"botconnection:*:{channelId}:*", grabCommands);
        }

        public async Task<BotConnection?> GetByLogin(string login, bool grabCommands)
        {
            return await GetBotConnection($"botconnection:*:*:{login}", grabCommands);
        }

        public async Task SaveOrUpdate(BotConnection botConnection)
        {
            await _redisClient.Db0.AddAsync($"botconnection:{botConnection.Id}:{botConnection.ChannelId}:{botConnection.Login}", botConnection);

            if(botConnection.ChannelCommands != null)
                await SetCommands(botConnection.Id, botConnection.ChannelCommands);
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

        public async Task SetCommand(Guid botConnectionId, Command command)
        {
            await _redisClient.Db0.AddAsync($"{botConnectionId}:commands:{command.Prefix}:definition", command);
        }

        public async Task SetCommands(Guid botConnectionId, ICollection<Command> commands)
        {
            await _redisClient.Db0.AddAllAsync(commands.Select(c => new Tuple<string, Command>($"{botConnectionId}:commands:{c.Prefix}:definition", c)).ToArray());
        }

        public async Task<ICollection<Command>> GetCommands(Guid botConnectionId)
        {
            var existingCommandKeys = await _redisClient.Db0.SearchKeysAsync($"{botConnectionId}:commands:*:definition");

            var commands = await _redisClient.Db0.GetAllAsync<Command>(new HashSet<string>(existingCommandKeys));

            return commands.Values;
        }

        public async Task<Command?> GetCommand(Guid botConnectionId, string commandPrefix)
        {
            return await _redisClient.Db0.GetAsync<Command>($"{botConnectionId}:commands:{commandPrefix}:definition");
        }
    }

    public interface IBotConnectionRepository
    {
        Task<BotConnection?> Get(string id, string channelId, string login);
        Task<ICollection<BotConnection>> GetAll(bool grabCommands = false);
        Task<BotConnection?> GetById(string id, bool grabCommands = false);
        Task<BotConnection?> GetByChannelId(string channelId, bool grabCommands = false);
        Task<BotConnection?> GetByLogin(string login, bool grabCommands = false);
        Task SaveOrUpdate(BotConnection botConnection);
        Task<IDictionary<string, long>> GetExecutionCounters(Guid botConnectionId);
        Task<IDictionary<string, long>> GetExecutionCounters();
        Task<DateTime> GetLastExecutionTime(Guid botConnectionId, string commandPrefix, string username);
        Task<DateTime> GetLastExecutionTime(Guid botConnectionId, string commandPrefix);
        Task SetLastExecutionTime(Guid botConnectionId, string commandPrefix, string username, DateTime time);
        void IncrementExecutionCounter(Guid botConnectionId, string commandPrefix);
        Task SetCommand(Guid botConnectionId, Command command);
        Task SetCommands(Guid botConnectionId, ICollection<Command> commands);
        Task<ICollection<Command>> GetCommands(Guid botConnectionId);
        Task<Command?> GetCommand(Guid botConnectionId, string commandPrefix);
    }
}
