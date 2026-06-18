using Entities;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    internal class CreateClipCommand : BaseCommand
    {
        public CreateClipCommand(BotConnection botConnection) : base(botConnection) { }

        public override string Prefix => Entities.Commands.CREATE_CLIP;
        public override string Syntax => "%clip";

        public override void InternalBuild(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = chatMessage.Username;
        }

        public override void InternalBuild(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }
    }
}
