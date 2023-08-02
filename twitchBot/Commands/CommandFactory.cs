using System;
using System.Collections.Generic;
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
            {Entities.Commands.LAST_MESSAGE, new LastMessageCommand(twitchApi, botConnection)},
            {Entities.Commands.GPT, new GptCommand(twitchApi, botConnection)},
            {Entities.Commands.NOTIFY, new NotifyCommand(twitchApi, botConnection)},
            {Entities.Commands.TTS, new TextToSpeechCommand(twitchApi, botConnection)},
            {Entities.Commands.EXISTING_COMMANDS, new ExistingCommandsCommand(twitchApi, botConnection)},
            {Entities.Commands.CREATE_CLIP, new CreateClipCommand(twitchApi, botConnection)},
            {Entities.Commands.COMMAND, new CommandCommand(twitchApi, botConnection)},
            {Entities.Commands.GPT_BEHAVIOR, new GptBehaviorCommand(twitchApi, botConnection)},
            {Entities.Commands.GPT_BEHAVIOR_DEFINITION, new GptBehaviorDefinitionCommand(twitchApi, botConnection)}
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
            if (!message.StartsWith("%") && !message.Contains("@kinobotz")) return null;
            if (message.Contains("@kinobotz"))
            {
                var messageContent = message.Replace("@kinobotz", string.Empty);

                var gptCommand = ChatCommands[Entities.Commands.GPT];

                gptCommand.Build(chatMessage, Entities.Commands.GPT, messageContent);

                return gptCommand;
            }

            var prefixStart = message.IndexOf('%');
            var prefixEnd = message.IndexOf(' ');

            string commandPrefix;
            if (prefixEnd > 0)
            {
                commandPrefix = message
                    .Substring(prefixStart, prefixEnd - prefixStart)
                    .Replace("%", string.Empty);
            }
            else
            {
                commandPrefix = message.Replace("%", string.Empty);
            }

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
