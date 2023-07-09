using Entities;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    internal class FirstFollowCommand : BaseCommand
    {
        public FirstFollowCommand(ITwitchAPI twitchApi, BotConnection botConnection) : base(twitchApi, botConnection) { }

        public override string Prefix => Entities.Commands.FIRST_FOLLOW;
        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            throw new System.NotImplementedException();
        }

        public override void Build(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }

        public ChatMessage ChatMessage { get; set; }
        public string Username { get; set; }
    }
}
