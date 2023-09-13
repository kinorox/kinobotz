using Entities;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class GptBehaviorCommand : BaseCommand
    {
        public GptBehaviorCommand(BotConnection botConnection) : base(botConnection)
        {
        }

        public string Behavior { get; set; }
        public string Channel { get; set; }

        public override string Prefix => Entities.Commands.GPT_BEHAVIOR;
        public override string Syntax => $"%{Prefix} <behaviorInstructions>";
        public override void InternalBuild(ChatMessage chatMessage, string command, string commandContent)
        {
            Behavior = commandContent;
            Username = chatMessage.Username;
            Channel = chatMessage.Channel;
        }

        public override void InternalBuild(RewardRedeemed rewardRedeemed)
        {
            Behavior = rewardRedeemed.Redemption.UserInput;
            Username = rewardRedeemed.Redemption.User.DisplayName;
            Channel = rewardRedeemed.Redemption.Reward.ChannelId;
        }
    }
}
