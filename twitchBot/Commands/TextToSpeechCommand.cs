using Entities;
using Infrastructure.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class TextToSpeechCommand : BaseCommand
    {
        public TextToSpeechCommand(BotConnection botConnection) : base(botConnection) { }

        public override string Prefix => Entities.Commands.TTS;
        public override string Syntax => $"%{Prefix} <voiceName>: <message>";
        public override void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Voice = commandContent.GetUntilOrEmpty(":").Trim().ToLower();
            Message = commandContent[(commandContent.IndexOf(':') + 1)..];
            Username = chatMessage.Username;
            Channel = chatMessage.Channel;
        }

        public override void Build(RewardRedeemed rewardRedeemed)
        {
            Voice = rewardRedeemed.Redemption.UserInput.GetUntilOrEmpty(":").Trim().ToLower();
            Message = rewardRedeemed.Redemption.UserInput[(rewardRedeemed.Redemption.UserInput.IndexOf(':') + 1)..];
            Username = rewardRedeemed.Redemption.User.DisplayName;
            Channel = rewardRedeemed.Redemption.Reward.ChannelId;
        }
        
        public string Channel { get; set; }
        public string Message { get; set; }
        public string Voice { get; set; }

    }
}
