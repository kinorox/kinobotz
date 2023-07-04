using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class GptCommand : BaseCommand
    {
        public GptCommand(ITwitchAPI twitchApi)
        {
            TwitchApi = twitchApi;
        }

        public override string Prefix => Commands.GPT;
        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Message = commandContent;
        }

        public override void Build(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }

        public string Message { get; set; }
    }
}
