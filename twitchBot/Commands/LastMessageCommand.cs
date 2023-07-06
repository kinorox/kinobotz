using Entities;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class LastMessageCommand : BaseCommand
    {
        public LastMessageCommand(ITwitchAPI twitchApi, BotConnection botConnection)
        {
            TwitchApi = twitchApi;
            BotConnection = botConnection;
        }

        public override string Prefix => Commands.LAST_MESSAGE;
        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = commandContent.Trim();
        }

        public override void Build(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }
        
        public string Username { get; set; }
    }
}
