using Entities;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Infrastructure.Repository
{
    public class CustomCommandRepository : ICustomCommandRepository
    {
        private readonly IRedisClient _redisClient;

        public CustomCommandRepository(IRedisClient redisClient)
        {
            _redisClient = redisClient;
        }

        public async Task CreateOrUpdate(Guid botConnectionId, string name, string content)
        {
            await _redisClient.Db0.AddAsync($"{botConnectionId}:{Commands.COMMAND}:{name}", content);
        }

        public async Task<string?> Get(Guid botConnectionId, string name)
        {
            return await _redisClient.Db0.GetAsync<string>($"{botConnectionId}:{Commands.COMMAND}:{name}");
        }

        public async Task Delete(Guid botConnectionId, string name)
        {
            await _redisClient.Db0.RemoveAsync($"{botConnectionId}:{Commands.COMMAND}:{name}");
        }
    }

    public interface ICustomCommandRepository
    {
        Task CreateOrUpdate(Guid botConnectionId, string name, string content);
        Task<string?> Get(Guid botConnectionId, string name);
        Task Delete(Guid botConnectionId, string name);
    }
}
