using System.Threading;
using System.Threading.Tasks;
using Entities;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class ExistingCommandsCommandHandler : BaseCommandHandler<ExistingCommandsCommand>
    {
        private readonly ICommandFactory commandFactory;
        private readonly IRedisClient redisClient;

        public ExistingCommandsCommandHandler(ICommandFactory commandFactory, IRedisClient redisClient) : base(redisClient)
        {
            this.commandFactory = commandFactory;
            this.redisClient = redisClient;
        }

        public override Task<Response> InternalHandle(ExistingCommandsCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Response()
            {
                Message = $"List of available commands: {string.Join(", ", commandFactory.GetChatCommandNames())}"
            });
        }
    }
}
