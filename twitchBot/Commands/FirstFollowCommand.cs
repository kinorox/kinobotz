using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    internal class FirstFollowCommand : ICommand
    {
        public string Prefix => Commands.FIRST_FOLLOW;
        public void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            throw new System.NotImplementedException();
        }

        public void Build(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }

        public ChatMessage ChatMessage { get; set; }
        public string Username { get; set; }
    }
}
