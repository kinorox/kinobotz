using System.Collections.Generic;
using Entities;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class DisableCommand : BaseCommand
    {
        public DisableCommand(BotConnection botConnection) : base(botConnection) { }

        public override string Prefix => Entities.Commands.DISABLE;
        public override string Syntax => $"%{Prefix} <commandName>";

        public string Command { get; set; }

        public override void InternalBuild(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = chatMessage.Username;
            Command = commandContent;
        }

        public override void InternalBuild(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }

        public override List<UserAccessLevelEnum> AccessLevels => new()
        {
            UserAccessLevelEnum.Admin,
            UserAccessLevelEnum.Broadcaster,
            UserAccessLevelEnum.Moderator
        };
    }
}
