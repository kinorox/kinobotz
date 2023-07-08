using System;
using Entities;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class CommandCommand : BaseCommand
    {
        public override string Prefix => Commands.COMMAND;

        public CommandCommand(ITwitchAPI twitchApi, BotConnection botConnection) : base(twitchApi, botConnection) { }

        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            if (string.IsNullOrEmpty(commandContent))
                throw new Exception("Invalid command syntax. Please follow the syntax: %command {add/update/delete} {content}");

            if (commandContent[0] == ' ')
                commandContent = commandContent[1..];

            var firstSpace = commandContent.IndexOf(' ');
            
            if (firstSpace < 0 || firstSpace > commandContent.Length)
                throw new Exception("Invalid command syntax. Please follow the syntax: %command {add/update/delete} {content}"); ;

            Operation = commandContent[..firstSpace]?.ToLower() switch
            {
                "add" => OperationEnum.Add,
                "update" => OperationEnum.Update,
                "delete" => OperationEnum.Delete,
                _ => throw new InvalidCommandOperationException()
            };

            var secondSpace = commandContent[(firstSpace + 1)..].IndexOf(' ');

            if (secondSpace < 0 || secondSpace > commandContent.Length)
                throw new Exception("Invalid command syntax. Please follow the syntax (without the brackets): %command {add/update/delete} {name} {content}");

            Name = commandContent[(firstSpace + 1)..][..secondSpace];
            Content = commandContent[(firstSpace + 1)..][(secondSpace + 1)..];
        }

        public override void Build(RewardRedeemed rewardRedeemed)
        {
            throw new NotImplementedException();
        }

        public OperationEnum Operation { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
    }

    public class InvalidCommandOperationException : Exception
    {
        public InvalidCommandOperationException() : base("Invalid command operation")
        {

        }
    }

    public enum OperationEnum
    {
        Add,
        Update,
        Delete
    }
}
