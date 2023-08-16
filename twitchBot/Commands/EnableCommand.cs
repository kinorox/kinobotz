using System.Collections.Generic;
using Entities;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class EnableCommand : BaseCommand
    {
        public EnableCommand(BotConnection botConnection) : base(botConnection) { }

        public override string Prefix => Entities.Commands.ENABLE;

        public string Command { get; set; }

        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = chatMessage.Username;
            Command = commandContent;
        }

        public override void Build(RewardRedeemed rewardRedeemed)
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
