using System.Threading;
using System.Threading.Tasks;
using twitchBot.Commands;
using twitchBot.Entities;

namespace twitchBot.Handlers
{
    public class ExistingCommandsCommandHandler : BaseCommandHandler<ExistingCommandsCommand>
    {
        private readonly ICommandFactory commandFactory;

        public ExistingCommandsCommandHandler(ICommandFactory commandFactory)
        {
            this.commandFactory = commandFactory;
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
