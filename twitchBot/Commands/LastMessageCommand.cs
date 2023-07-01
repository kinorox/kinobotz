using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class LastMessageCommand : ICommand
    {
        public string Prefix => Commands.LAST_MESSAGE;
        public void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = commandContent;
        }

        public void Build(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }

        public ChatMessage ChatMessage { get; set; }
        public string Username { get; set; }
    }
}
