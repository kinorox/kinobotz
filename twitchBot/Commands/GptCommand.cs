using TwitchLib.Client.Models;

namespace twitchBot.Commands
{
    public class GptCommand : ICommand
    {
        public string Prefix => Commands.GPT;
        public void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Message = commandContent;
        }

        public string Message { get; set; }
    }
}
