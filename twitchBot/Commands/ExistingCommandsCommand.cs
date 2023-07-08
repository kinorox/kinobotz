using Entities;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class ExistingCommandsCommand : BaseCommand
    {
        public ExistingCommandsCommand(ITwitchAPI twitchApi, BotConnection botConnection) : base(twitchApi, botConnection) { }

        public override string Prefix => Commands.EXISTING_COMMANDS;
        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
        }
        public override void Build(RewardRedeemed rewardRedeemed)
        {
        }
    }
}
