﻿using Entities;
using System.Collections.Generic;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class UpdateStreamTitleCommmand : BaseCommand
    {
        public UpdateStreamTitleCommmand(BotConnection botConnection) : base(botConnection)
        {
        }

        public override List<UserAccessLevelEnum> AccessLevels { get; } = new()
        {
            UserAccessLevelEnum.Broadcaster,
            UserAccessLevelEnum.Moderator,
            UserAccessLevelEnum.Admin
        };

        public override string Syntax => $"%{Prefix} <title>";

        public override string Prefix => Entities.Commands.UPDATE_STREAM_TITLE;
        
        public string Title { get; set; }

        public override void InternalBuild(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = chatMessage.Username;
            Title = commandContent;
        }

        public override void InternalBuild(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }
    }
}
