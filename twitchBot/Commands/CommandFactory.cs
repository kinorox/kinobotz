using System.Collections.Generic;
using System.Linq;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class CommandFactory : ICommandFactory
    {
        private Dictionary<string, ICommand> ChatCommands => new()
        {
            {Commands.LAST_MESSAGE, new LastMessageCommand()},
            {Commands.GPT, new GptCommand()},
            {Commands.NOTIFY, new NotifyCommand()},
            {Commands.TTS, new TextToSpeechCommand()}
        };

        private Dictionary<string, ICommand> RewardCommands => new()
        {
        };

        public ICommand Build(ChatMessage chatMessage)
        {
            var message = chatMessage.Message;

            // Check if message has the default command prefix
            if (!message.StartsWith("%"))
                return null;

            // Retrieve command prefix from message
            string commandPrefix = message
                .Substring(0, ChatCommands.Keys.Max(k => k.Length) + 1)
                .Split(' ')[0]
                .Replace("%", string.Empty);

            if (!ChatCommands.TryGetValue(commandPrefix, out var command)) return null;
            command.Build(chatMessage, commandPrefix, message.Split($"%{commandPrefix}")[1]);
            return command;
        }

        public ICommand Build(RewardRedeemed rewardRedeemed)
        {
            var rewardTitle = rewardRedeemed.Redemption.Reward.Title;

            var commandPrefix = rewardTitle switch
            {
                "TTS V2" => "tts",
                _ => null
            };

            if (commandPrefix == null) return null;

            _ = !RewardCommands.TryGetValue(commandPrefix, out var command);

            if (command == null) return null;

            command.Build(rewardRedeemed);

            return command;
        }
    }

    public interface ICommandFactory
    {
        ICommand Build(ChatMessage chatMessage);
        ICommand Build(RewardRedeemed rewardRedeemed);
    }
}
