using Entities;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    internal class FirstFollowCommand : BaseCommand
    {
        public FirstFollowCommand(BotConnection botConnection) : base(botConnection) { }

        public override string Prefix => Entities.Commands.FIRST_FOLLOW;
        public override string Syntax => $"%{Prefix}";
        public override void InternalBuild(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = chatMessage.Username;
        }

        public override void InternalBuild(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }

        public ChatMessage ChatMessage { get; set; }
    }
}
