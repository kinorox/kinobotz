using System;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using MediatR;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class GptBehaviorCommandHandler : BaseCommandHandler<GptBehaviorCommand>
    {
        private readonly IMediator mediator;
        private readonly IGptRepository gptRepository;

        public GptBehaviorCommandHandler(IRedisClient redisClient, IMediator mediator, IGptRepository gptRepository) : base(redisClient)
        {
            this.mediator = mediator;
            this.gptRepository = gptRepository;
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

            await gptRepository.SetGptBehaviorDefinedBy(request.BotConnection.Id.ToString(), request.Username);
            await gptRepository.SetGptBehavior(request.BotConnection.Id.ToString(), new BehaviorDefinition(request.Behavior, request.Username, DateTime.UtcNow, request.Channel));

            return new Response()
            {
                Message = "GPT behavior set."
            };
        }
    }
}
