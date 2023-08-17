using System;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using MediatR;
using Microsoft.Extensions.Configuration;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class GptBehaviorCommandHandler : BaseCommandHandler<GptBehaviorCommand>
    {
        private readonly IMediator _mediator;
        private readonly IGptRepository _gptRepository;

        public GptBehaviorCommandHandler(IMediator mediator, IGptRepository gptRepository, IBotConnectionRepository botConnectionRepository, IConfiguration configuration, ICommandRepository commandRepository) : base(botConnectionRepository, configuration, commandRepository)
        {
            _mediator = mediator;
            _gptRepository = gptRepository;
        }
        public override int Cooldown => 10;
        public override bool GlobalCooldown => true;

        public override async Task<Response> InternalHandle(GptBehaviorCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Behavior))
            {
                var gptBehaviorDefinitionCommand =
                    new GptBehaviorDefinitionCommand(request.BotConnection)
                    {
                        Username = request.Username
                    };

                var response = await _mediator.Send(gptBehaviorDefinitionCommand, cancellationToken);

                response.WasExecuted = false;

                return response;
            }

            await _gptRepository.SetGptBehaviorDefinedBy(request.BotConnection.Id.ToString(), request.Username);
            await _gptRepository.SetGptBehavior(request.BotConnection.Id.ToString(), new BehaviorDefinition(request.Behavior, request.Username, DateTime.UtcNow, request.Channel));

            return new Response()
            {
                Message = "GPT behavior set."
            };
        }
    }
}
