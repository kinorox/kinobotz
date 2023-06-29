using TwitchLib.Client.Models;

namespace twitchBot.Commands
{
    internal class FirstFollowCommand : ICommand
    {
        public string Prefix => Commands.FIRST_FOLLOW;
        public void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            throw new System.NotImplementedException();
        }
        public ChatMessage ChatMessage { get; set; }
        public string Username { get; set; }
    }
}
