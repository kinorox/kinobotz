using TwitchLib.Client.Models;

namespace twitchBot.Commands
{
    public class LastMessageCommand : ICommand
    {
        public string Prefix => Entities.Commands.LAST_MESSAGE;
        public ChatMessage ChatMessage { get; set; }
        public string Username { get; set; }
    }
}
