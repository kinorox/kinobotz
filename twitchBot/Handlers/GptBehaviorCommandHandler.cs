using System.Threading;
using System.Threading.Tasks;
using Entities;
using MediatR;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class GptBehaviorCommandHandler : BaseCommandHandler<GptBehaviorCommand>
    {
        private readonly IRedisClient redisClient;
        private readonly IMediator mediator;

        public GptBehaviorCommandHandler(IRedisClient redisClient, IMediator mediator) : base(redisClient)
        {
            this.redisClient = redisClient;
            this.mediator = mediator;
        }
        public override int Cooldown => 10;
        public override bool GlobalCooldown => true;

        public override async Task<Response> InternalHandle(GptBehaviorCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Behavior))
            {
                var gptBehaviorDefinitionCommand =
                    new GptBehaviorDefinitionCommand(request.TwitchApi, request.BotConnection)
                    {
                        Username = request.Username
                    };

                var response = await mediator.Send(gptBehaviorDefinitionCommand, cancellationToken);

                response.WasExecuted = false;

                return response;
            }

            await redisClient.Db0.AddAsync($"{request.BotConnection.Id}:{request.Prefix}:definedby", request.Username);
            await redisClient.Db0.AddAsync($"{request.BotConnection.Id}:{request.Prefix}", request.Behavior);

            return new Response()
            {
                Message = "GPT behavior set."
            };
        }
    }
}
