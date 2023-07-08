using Entities;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public abstract class BaseCommand : ICommand
    {
        protected BaseCommand(ITwitchAPI twitchApi, BotConnection botConnection)
        {
            TwitchApi = twitchApi;
            BotConnection = botConnection;
        }
        
        public abstract string Prefix { get; }
        public abstract void Build(ChatMessage chatMessage, string command, string commandContent);
        public abstract void Build(RewardRedeemed rewardRedeemed);
        public ITwitchAPI TwitchApi { get; set; }
        public BotConnection BotConnection { get; set; }
    }
}
