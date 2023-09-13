using Entities;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class GptCommand : BaseCommand
    {
        public GptCommand(BotConnection botConnection) : base(botConnection) { }

        public override string Prefix => Entities.Commands.GPT;
        public override string Syntax => $"%{Prefix} or mention @kinobotz";
        public override void InternalBuild(ChatMessage chatMessage, string command, string commandContent)
        {
            Message = commandContent; 
            Username = chatMessage.Username;
        }

        public override void InternalBuild(RewardRedeemed rewardRedeemed)
        {
            throw new System.NotImplementedException();
        }

        public string Message { get; set; }
    }
}
