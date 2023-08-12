using System.Collections.Generic;
using Entities;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public abstract class BaseCommand : ICommand
    {
        protected BaseCommand(BotConnection botConnection)
        {
            BotConnection = botConnection;
        }
        
        public abstract string Prefix { get; }
        public abstract void Build(ChatMessage chatMessage, string command, string commandContent);
        public abstract void Build(RewardRedeemed rewardRedeemed);
        public BotConnection BotConnection { get; set; }
        public string Username { get; set; }
        public virtual List<UserAccessLevelEnum> AccessLevels { get; } = new()
        {
            UserAccessLevelEnum.Default,
            UserAccessLevelEnum.Admin,
            UserAccessLevelEnum.Broadcaster,
            UserAccessLevelEnum.Moderator,
            UserAccessLevelEnum.Subscriber
        };
    }
}
