using System.Threading;
using System.Threading.Tasks;
using Entities;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class GptBehaviorCommandHandler : BaseCommandHandler<GptBehaviorCommand>
    {
        private readonly IRedisClient redisClient;

        public GptBehaviorCommandHandler(IRedisClient redisClient) : base(redisClient)
        {
            this.redisClient = redisClient;
        }
        public override int Cooldown => 10;
        public override bool GlobalCooldown => true;

        public override async Task<Response> InternalHandle(GptBehaviorCommand request, CancellationToken cancellationToken)
        {
            await redisClient.Db0.AddAsync($"{request.BotConnection.Id}:{request.Prefix}:definedby", request.Username);
            await redisClient.Db0.AddAsync($"{request.BotConnection.Id}:{request.Prefix}", request.Behavior);

            return new Response()
            {
                Message = "GPT behavior set."
            };
        }
    }
}
