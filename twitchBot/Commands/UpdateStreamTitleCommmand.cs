using Entities;
using System.Collections.Generic;
using twitchBot.Handlers;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class UpdateStreamTitleCommmand : BaseCommand
    {
        public UpdateStreamTitleCommmand(ITwitchAPI twitchApi, BotConnection botConnection) : base(twitchApi, botConnection)
        {
        }

        public override List<UserAccessLevelEnum> AccessLevels { get; } = new()
        {
            UserAccessLevelEnum.Broadcaster,
            UserAccessLevelEnum.Moderator,
            UserAccessLevelEnum.Admin
        };

        public override string Prefix => Entities.Commands.UPDATE_STREAM_TITLE;
        
        public string Title { get; set; }

        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = chatMessage.Username;
            Title = commandContent;
        }

        public override void Build(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }
    }
}
