using twitchBot.Utils;
using TwitchLib.Client.Models;

namespace twitchBot.Commands
{
    public class TextToSpeechCommand : ICommand
    {
        public string Prefix => Commands.TTS;
        public void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Voice = commandContent.GetUntilOrEmpty(":");
            Message = commandContent;
        }

        public string Message { get; set; }
        public string Voice { get; set; }

    }
}
