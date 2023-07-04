using System.Collections.Generic;
using System.Linq;
using Entities;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class CommandFactory : ICommandFactory
    {
        private ITwitchAPI twitchApi;
        private BotConnection botConnection;

        private Dictionary<string, ICommand> ChatCommands => new()
        {
            {Commands.LAST_MESSAGE, new LastMessageCommand(twitchApi, botConnection)},
            {Commands.GPT, new GptCommand(twitchApi, botConnection)},
            {Commands.NOTIFY, new NotifyCommand(twitchApi, botConnection)},
            {Commands.TTS, new TextToSpeechCommand(twitchApi, botConnection)},
            {Commands.EXISTING_COMMANDS, new ExistingCommandsCommand(twitchApi, botConnection)},
            {Commands.CREATE_CLIP, new CreateClipCommand(twitchApi, botConnection)}
        };

        private Dictionary<string, ICommand> RewardCommands => new()
        {
        };

        public void Setup(ITwitchAPI twitchApi, BotConnection botConnection)
        {
            this.twitchApi = twitchApi;
            this.botConnection = botConnection;
        }

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

        public IEnumerable<string> GetChatCommandNames()
        {
            return ChatCommands.Keys;
        }
    }

    public interface ICommandFactory
    {
        void Setup(ITwitchAPI twitchApi, BotConnection botConnection);
        ICommand Build(ChatMessage chatMessage);
        ICommand Build(RewardRedeemed rewardRedeemed);
        IEnumerable<string> GetChatCommandNames();
    }
}
