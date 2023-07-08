using System.Threading;
using System.Threading.Tasks;
using twitchBot.Commands;
using twitchBot.Entities;

namespace twitchBot.Handlers
{
    public class CommandCommandHandler : BaseCommandHandler<CommandCommand>
    {
        public override async Task<Response> InternalHandle(CommandCommand request, CancellationToken cancellationToken)
        {
            return new Response()
            {
                Message = "test"
            };
        }
    }
}
