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
        private BotConnection _botConnection;

        private Dictionary<string, BaseCommand> ChatCommands => new()
        {
            {Entities.Commands.LAST_MESSAGE, new LastMessageCommand(_botConnection)},
            {Entities.Commands.GPT, new GptCommand(_botConnection)},
            {Entities.Commands.NOTIFY, new NotifyCommand(_botConnection)},
            {Entities.Commands.TTS, new TextToSpeechCommand(_botConnection)},
            {Entities.Commands.EXISTING_COMMANDS, new ExistingCommandsCommand(_botConnection)},
            {Entities.Commands.CREATE_CLIP, new CreateClipCommand(_botConnection)},
            {Entities.Commands.COMMAND, new CommandCommand(_botConnection)},
            {Entities.Commands.GPT_BEHAVIOR, new GptBehaviorCommand(_botConnection)},
            {Entities.Commands.GPT_BEHAVIOR_DEFINITION, new GptBehaviorDefinitionCommand(_botConnection)},
            {Entities.Commands.RANDOM_STREAM_TITLE, new GenerateRandomStreamTitleCommand(_botConnection)},
            {Entities.Commands.UPDATE_STREAM_TITLE, new UpdateStreamTitleCommmand(_botConnection)},
            {Entities.Commands.ENABLE, new EnableCommand(_botConnection)},
            {Entities.Commands.DISABLE, new DisableCommand(_botConnection)}
        };

        private Dictionary<string, ICommand> RewardCommands => new()
        {
        };

        public void Setup(ITwitchAPI twitchApi, BotConnection botConnection)
        {
            _botConnection = botConnection;
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

            if (chatMessage.IsModerator)
            {
                command.UserAccessLevel = UserAccessLevelEnum.Moderator;
            }
            else if (chatMessage.IsBroadcaster)
            {
                command.UserAccessLevel = UserAccessLevelEnum.Broadcaster;
            } 
            else if (chatMessage.IsSubscriber)
            {
                command.UserAccessLevel = UserAccessLevelEnum.Subscriber;
            } 
            else if (chatMessage.IsVip)
            {
                command.UserAccessLevel = UserAccessLevelEnum.Vip;
            } 
            else
            {
                command.UserAccessLevel = UserAccessLevelEnum.Default;
            }

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
