using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class GptBehaviorDefinitionCommandHandler : BaseCommandHandler<GptBehaviorDefinitionCommand>
    {
        private readonly IGptRepository _gptRepository;

        public GptBehaviorDefinitionCommandHandler(IRedisClient redisClient, IGptRepository gptRepository, IBotConnectionRepository botConnectionRepository) : base(redisClient, botConnectionRepository)
        {
            _gptRepository = gptRepository;
        }

        public override async Task<Response> InternalHandle(GptBehaviorDefinitionCommand request, CancellationToken cancellationToken)
        {
            var userName = await _gptRepository.GetGptBehaviorDefinedBy(request.BotConnection.Id.ToString());
            var behavior = await _gptRepository.GetGptBehavior(request.BotConnection.Id.ToString());

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
