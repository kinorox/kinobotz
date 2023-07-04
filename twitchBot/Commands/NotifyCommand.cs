using Entities;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class NotifyCommand : BaseCommand
    {
        public NotifyCommand(ITwitchAPI twitchApi, BotConnection botConnection)
        {
            TwitchApi = twitchApi;
            BotConnection = botConnection;
        }

        public override string Prefix => Commands.NOTIFY;

        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = chatMessage.Username;
        }

        public override void Build(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }
        
        public string Username { get; set; }
    }
}
