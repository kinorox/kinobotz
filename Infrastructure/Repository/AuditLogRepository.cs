using Entities;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Infrastructure.Repository
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly IRedisClient _redisClient;

        public AuditLogRepository(IRedisClient redisClient)
        {
            _redisClient = redisClient;
        }

        public async Task Create(Guid botConnectionId, AuditLog auditLog)
        {
            await _redisClient.Db0.SetAddAsync("auditlog", auditLog);
            await _redisClient.Db0.SetAddAsync($"{botConnectionId}:auditlog", auditLog);
        }

        public async Task<AuditLog[]> Get(Guid botConnectionId)
        {
            return await _redisClient.Db0.SetMembersAsync<AuditLog>($"{botConnectionId}:auditlog");
        }

        public async Task<AuditLog[]> Get()
        {
            return await _redisClient.Db0.SetMembersAsync<AuditLog>("auditlog");
        }
    }

    public interface IAuditLogRepository
    {
        Task Create(Guid botConnectionId, AuditLog auditLog);
        Task<AuditLog[]> Get(Guid botConnectionId);
        Task<AuditLog[]> Get();
    }
}
