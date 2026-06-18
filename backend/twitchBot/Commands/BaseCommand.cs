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
        public abstract string Syntax { get; }
        public abstract void InternalBuild(ChatMessage chatMessage, string command, string commandContent);
        public abstract void InternalBuild(RewardRedeemed rewardRedeemed);

        public void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            ChatMessage = chatMessage;
            InternalBuild(chatMessage, command, commandContent);
        }

        public void Build(RewardRedeemed rewardRedeemed)
        {
            RewardRedeemed = rewardRedeemed;
            InternalBuild(rewardRedeemed);
        }

        public BotConnection BotConnection { get; set; }
        public string Username { get; set; }
        public UserAccessLevelEnum UserAccessLevel { get; set; }
        public ChatMessage ChatMessage { get; set; }
        public RewardRedeemed RewardRedeemed { get; set; }
        public virtual List<UserAccessLevelEnum> AccessLevels { get; } = new()
        {
            UserAccessLevelEnum.Default,
            UserAccessLevelEnum.Admin,
            UserAccessLevelEnum.Broadcaster,
            UserAccessLevelEnum.Moderator,
            UserAccessLevelEnum.Subscriber,
            UserAccessLevelEnum.Vip
        };
    }
}
