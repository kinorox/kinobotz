using Entities;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class LastMessageCommand : BaseCommand
    {
        public LastMessageCommand(BotConnection botConnection) : base(botConnection) { }

        public override string Prefix => Entities.Commands.LAST_MESSAGE;
        public override string Syntax => $"%{Prefix} <username>";
        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            TargetUsername = commandContent.Trim();
            Username = chatMessage.Username;
        }

        public override void Build(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }
        
        public string TargetUsername { get; set; }
    }
}
