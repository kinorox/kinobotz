using MediatR;
using twitchBot.Entities;
using ChatMessage = TwitchLib.Client.Models.ChatMessage;

namespace twitchBot.Commands
{
    public interface ICommand : IRequest<Response>
    {
        public string Prefix { get; }
        void Build(ChatMessage chatMessage, string command, string commandContent);
    }
}
