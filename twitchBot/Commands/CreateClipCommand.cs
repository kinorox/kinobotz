using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    internal class CreateClipCommand : ICommand
    {
        public string Prefix => Commands.CREATE_CLIP;
        public void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Name = commandContent;
        }

        public void Build(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }
        
        public string Name { get; set; }
    }
}
