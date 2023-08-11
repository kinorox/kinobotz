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

        public async Task<BotConnection?> GetById(string id)
        {
            return await _redisClient.Db0.GetAsync<BotConnection>($"botconnection:{id}:*:*");
        }

        public async Task<BotConnection?> GetByChannelId(string channelId)
        {
            return await _redisClient.Db0.GetAsync<BotConnection>($"botconnection:*:{channelId}:*");
        }

        public async Task<BotConnection?> GetByLogin(string login)
        {
            return await _redisClient.Db0.GetAsync<BotConnection>($"botconnection:*:*:{login}");
        }

        public async Task SaveOrUpdate(BotConnection botConnection)
        {
            await _redisClient.Db0.AddAsync($"botconnection:{botConnection.Id}:{botConnection.ChannelId}:{botConnection.Login}", botConnection);
        }
    }

    public interface IBotConnectionRepository
    {
        Task<BotConnection?> GetById(string id);
        Task<BotConnection?> GetByChannelId(string channelId);
        Task<BotConnection?> GetByLogin(string login);
        Task SaveOrUpdate(BotConnection botConnection);
    }
}
