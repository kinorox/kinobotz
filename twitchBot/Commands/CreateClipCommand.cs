using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    internal class CreateClipCommand : BaseCommand
    {
        public CreateClipCommand(ITwitchAPI twitchApi)
        {
            TwitchApi = twitchApi;
        }

        public override string Prefix => Commands.CREATE_CLIP;
        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
        }

        public override void Build(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }
    }
}
