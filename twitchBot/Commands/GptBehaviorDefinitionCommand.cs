using System;
using Entities;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class GptBehaviorDefinitionCommand : BaseCommand
    {
        public GptBehaviorDefinitionCommand(ITwitchAPI twitchApi, BotConnection botConnection) : base(twitchApi, botConnection)
        {
        }

        public override string Prefix => Entities.Commands.GPT_BEHAVIOR_DEFINITION;
        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = chatMessage.Username;
        }

        public override void Build(RewardRedeemed rewardRedeemed)
        {
            throw new NotImplementedException();
        }
    }
}
