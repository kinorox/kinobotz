using Entities;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class ExistingCommandsCommand : BaseCommand
    {
        public ExistingCommandsCommand(BotConnection botConnection) : base(botConnection) { }

        public override string Prefix => Entities.Commands.EXISTING_COMMANDS;
        public override string Syntax => $"%{Prefix}";
        public override void InternalBuild(ChatMessage chatMessage, string command, string commandContent)
        {
            Username = chatMessage.Username;
        }
        public override void InternalBuild(RewardRedeemed rewardRedeemed)
        {
        }
    }
}
