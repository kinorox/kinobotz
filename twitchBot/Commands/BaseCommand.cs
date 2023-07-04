using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public abstract class BaseCommand : ICommand
    {
        public abstract string Prefix { get; }
        public abstract void Build(ChatMessage chatMessage, string command, string commandContent);
        public abstract void Build(RewardRedeemed rewardRedeemed);
        public ITwitchAPI TwitchApi { get; set; }
    }
}
