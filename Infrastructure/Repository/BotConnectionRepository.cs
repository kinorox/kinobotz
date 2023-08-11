using Entities;
using StackExchange.Redis.Extensions.Core.Abstractions;

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

        public async Task<ICollection<BotConnection>> GetAll()
        {
            var existingKeys = await _redisClient.Db0.SearchKeysAsync("botconnection:*:*:*");

            var botConnections = await _redisClient.Db0.GetAllAsync<BotConnection>(new HashSet<string>(existingKeys));

            return botConnections.Values;
        }

        public async Task<BotConnection?> GetById(string id)
        {
            var existingKeys = await _redisClient.Db0.SearchKeysAsync($"botconnection:{id}:*:*");
            
            if(!existingKeys.Any())
                return null;

            return await _redisClient.Db0.GetAsync<BotConnection>(existingKeys.FirstOrDefault());
        }

        public async Task<BotConnection?> GetByChannelId(string channelId)
        {
            var existingKeys = await _redisClient.Db0.SearchKeysAsync($"botconnection:*:{channelId}:*");

            if (!existingKeys.Any())
                return null;

            return await _redisClient.Db0.GetAsync<BotConnection>(existingKeys.FirstOrDefault());
        }

        public async Task<BotConnection?> GetByLogin(string login)
        {
            var existingKeys = await _redisClient.Db0.SearchKeysAsync($"botconnection:*:*:{login}");

            if (!existingKeys.Any())
                return null;

            return await _redisClient.Db0.GetAsync<BotConnection>(existingKeys.FirstOrDefault());
        }

        public async Task SaveOrUpdate(BotConnection botConnection)
        {
            await _redisClient.Db0.AddAsync($"botconnection:{botConnection.Id}:{botConnection.ChannelId}:{botConnection.Login}", botConnection);
        }
    }

    public interface IBotConnectionRepository
    {
        Task<BotConnection?> Get(string id, string channelId, string login);
        Task<ICollection<BotConnection>> GetAll();
        Task<BotConnection?> GetById(string id);
        Task<BotConnection?> GetByChannelId(string channelId);
        Task<BotConnection?> GetByLogin(string login);
        Task SaveOrUpdate(BotConnection botConnection);
    }
}
