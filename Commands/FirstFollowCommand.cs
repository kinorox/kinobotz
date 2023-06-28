using TwitchLib.Client.Models;

namespace twitchBot.Commands
{
    internal class FirstFollowCommand : ICommand
    {
        public string Prefix => Entities.Commands.FIRST_FOLLOW;
        public ChatMessage ChatMessage { get; set; }
        public string Username { get; set; }
    }
}
