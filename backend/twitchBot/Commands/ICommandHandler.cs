using System.Threading;
using System.Threading.Tasks;
using Entities;

namespace twitchBot.Commands
{
    /// <summary>Handles a single command type. Replaces MediatR's IRequestHandler.</summary>
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        Task<Response> Handle(TCommand command, CancellationToken cancellationToken);
    }
}
