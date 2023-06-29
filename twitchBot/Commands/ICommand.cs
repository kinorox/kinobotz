using MediatR;
using twitchBot.Entities;

namespace twitchBot.Commands
{
    public interface ICommand : IRequest<Response>
    {
        public string Prefix { get; }
    }
}
