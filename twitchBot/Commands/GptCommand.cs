using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class GptCommand : ICommand
    {
        public string Prefix => Commands.GPT;
        public void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Message = commandContent;
        }

        public void Build(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }

        public string Message { get; set; }
    }
}
