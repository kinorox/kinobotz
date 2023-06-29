using System.Collections.Generic;
using System.Linq;
using TwitchLib.Client.Models;

namespace twitchBot.Commands
{
    public static class CommandFactory
    {
        private static Dictionary<string, ICommand> Commands => new()
        {
            {twitchBot.Commands.Commands.LAST_MESSAGE, new LastMessageCommand()},
            {twitchBot.Commands.Commands.GPT, new GptCommand()},
            {twitchBot.Commands.Commands.TTS, new TextToSpeechCommand()}
        };

        public static ICommand Build(ChatMessage chatMessage)
        {
            var message = chatMessage.Message;

            // Check if message has the default command prefix
            if (!message.StartsWith("%"))
                return null;

            // Retrieve command prefix from message
            string commandPrefix = message
                .Substring(0, Commands.Keys.Max(k => k.Length) + 1)
                .Split(' ')[0]
                .Replace("%", string.Empty);

            if (!Commands.TryGetValue(commandPrefix, out var command)) return null;
            command.Build(chatMessage, commandPrefix, message.Split($"%{commandPrefix}")[1]);
            return command;
        }
    }
}
