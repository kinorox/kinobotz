﻿using Entities;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class GptBehaviorCommand : BaseCommand
    {
        public GptBehaviorCommand(ITwitchAPI twitchApi, BotConnection botConnection) : base(twitchApi, botConnection)
        {
        }

        public string Behavior { get; set; }
        public string Channel { get; set; }

        public override string Prefix => Entities.Commands.GPT_BEHAVIOR;
        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Behavior = commandContent;
            Username = chatMessage.Username;
            Channel = chatMessage.Channel;
        }

        public override void Build(RewardRedeemed rewardRedeemed)
        {
            Behavior = rewardRedeemed.Redemption.UserInput;
            Username = rewardRedeemed.Redemption.User.DisplayName;
            Channel = rewardRedeemed.Redemption.Reward.ChannelId;
        }
    }
}