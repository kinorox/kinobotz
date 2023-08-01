using System.Threading;
using System.Threading.Tasks;
using Entities;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class GptBehaviorDefinitionCommandHandler : BaseCommandHandler<GptBehaviorDefinitionCommand>
    {
        private readonly IRedisClient redisClient;

        public GptBehaviorDefinitionCommandHandler(IRedisClient redisClient) : base(redisClient)
        {
            this.redisClient = redisClient;
        }

        public override async Task<Response> InternalHandle(GptBehaviorDefinitionCommand request, CancellationToken cancellationToken)
        {
            var userName = await redisClient.Db0.GetAsync<string>($"{request.BotConnection.Id}:{Entities.Commands.GPT_BEHAVIOR}:definedby");
            var behavior = await redisClient.Db0.GetAsync<string>($"{request.BotConnection.Id}:{Entities.Commands.GPT_BEHAVIOR}");

            var message = $"Current behavior defined by {userName}: {behavior}";

            if (message.Length >= 500)
            {
                return new Response()
                {
                    Message = message[..500]
                };
            }

            return new Response()
            {
                Message = message
            };
        }
    }
}
