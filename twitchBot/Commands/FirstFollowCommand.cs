using Entities;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    internal class FirstFollowCommand : BaseCommand
    {
        public FirstFollowCommand(BotConnection botConnection) : base(botConnection) { }

        public override string Prefix => Entities.Commands.FIRST_FOLLOW;
        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = chatMessage.Username;
        }

        public override void Build(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }

        public ChatMessage ChatMessage { get; set; }
    }
}
