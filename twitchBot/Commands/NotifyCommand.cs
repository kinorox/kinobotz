using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class NotifyCommand : ICommand
    {
        public string Prefix => Commands.NOTIFY;

        public void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = chatMessage.Username;
        }

        public void Build(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }
        
        public string Username { get; set; }
    }
}
