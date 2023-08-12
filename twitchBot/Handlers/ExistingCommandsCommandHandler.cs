using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class ExistingCommandsCommandHandler : BaseCommandHandler<ExistingCommandsCommand>
    {
        private readonly ICommandFactory _commandFactory;
        private readonly IRedisClient _redisClient;

        public ExistingCommandsCommandHandler(ICommandFactory commandFactory, IRedisClient redisClient, IBotConnectionRepository botConnectionRepository, IConfiguration configuration) : base(redisClient, botConnectionRepository, configuration)
        {
            _commandFactory = commandFactory;
            _redisClient = redisClient;
        }

        public override Task<Response> InternalHandle(ExistingCommandsCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Response()
            {
                Message = $"List of available commands: {string.Join(", ", _commandFactory.GetChatCommandNames())}"
            });
        }
    }
}
