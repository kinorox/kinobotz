using System.Collections.Generic;
using Entities;
using twitchBot.Handlers;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class GenerateRandomStreamTitleCommand : BaseCommand
    {
        public GenerateRandomStreamTitleCommand(ITwitchAPI twitchApi, BotConnection botConnection) : base(twitchApi, botConnection)
        {
        }

        public override string Prefix => Entities.Commands.RANDOM_STREAM_TITLE;
        
        public override List<UserAccessLevelEnum> AccessLevels { get; } = new()
        {
            UserAccessLevelEnum.Broadcaster,
            UserAccessLevelEnum.Moderator,
            UserAccessLevelEnum.Admin
        };

        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = chatMessage.Username;
        }

        public override void Build(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }
    }
}
