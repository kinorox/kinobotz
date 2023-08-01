using System;
using Entities;
using Entities.Exceptions;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class CommandCommand : BaseCommand
    {
        private readonly string errorMessage = "Please follow the syntax (without the brackets): %command {add/update/delete} {name} {content}";

        public override string Prefix => Entities.Commands.COMMAND;

        public CommandCommand(ITwitchAPI twitchApi, BotConnection botConnection) : base(twitchApi, botConnection) { }

        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = chatMessage.Username;

            if (string.IsNullOrEmpty(commandContent))
                throw new InvalidCommandException(errorMessage);

            if (commandContent[0] == ' ')
                commandContent = commandContent[1..];

            var firstSpace = commandContent.IndexOf(' ');
            
            if (firstSpace < 0 || firstSpace > commandContent.Length)
                throw new InvalidCommandException(errorMessage); 

            Operation = commandContent[..firstSpace]?.ToLower() switch
            {
                "add" => OperationEnum.Add,
                "update" => OperationEnum.Update,
                "delete" => OperationEnum.Delete,
                _ => throw new InvalidCommandOperationException("Available operations: add, update, delete.")
            };

            var secondSpace = commandContent[(firstSpace + 1)..].IndexOf(' ');

            if (secondSpace < 0 || secondSpace > commandContent.Length)
                throw new InvalidCommandException(errorMessage);

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

    public enum OperationEnum
    {
        Add,
        Update,
        Delete
    }
}
