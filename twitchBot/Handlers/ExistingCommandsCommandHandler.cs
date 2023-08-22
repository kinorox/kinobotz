using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class ExistingCommandsCommandHandler : BaseCommandHandler<ExistingCommandsCommand>
    {
        private readonly ICommandFactory _commandFactory;

        public ExistingCommandsCommandHandler(ICommandFactory commandFactory, IBotConnectionRepository botConnectionRepository, IConfiguration configuration) : base(botConnectionRepository, configuration)
        {
            _commandFactory = commandFactory;
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
