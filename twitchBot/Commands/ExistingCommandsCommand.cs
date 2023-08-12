using Entities;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class ExistingCommandsCommand : BaseCommand
    {
        public ExistingCommandsCommand(BotConnection botConnection) : base(botConnection) { }

        public override string Prefix => Entities.Commands.EXISTING_COMMANDS;
        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = chatMessage.Username;
        }
        public override void Build(RewardRedeemed rewardRedeemed)
        {
        }
    }
}
