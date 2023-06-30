using twitchBot.Utils;
using TwitchLib.Client.Models;

namespace twitchBot.Commands
{
    public class TextToSpeechCommand : ICommand
    {
        public string Prefix => Commands.TTS;
        public void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Voice = commandContent.GetUntilOrEmpty(":").Trim().ToLower();
            Message = commandContent[(commandContent.IndexOf(':') + 1)..];
            ChatMessage = chatMessage;
        }

        public ChatMessage ChatMessage { get; set; }
        public string Message { get; set; }
        public string Voice { get; set; }

    }
}
