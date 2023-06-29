using TwitchLib.Client.Models;

namespace twitchBot.Commands
{
    public class LastMessageCommand : ICommand
    {
        public string Prefix => Commands.LAST_MESSAGE;
        public void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = commandContent;
        }
        
        public ChatMessage ChatMessage { get; set; }
        public string Username { get; set; }
    }
}
