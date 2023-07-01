using twitchBot.Utils;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace twitchBot.Commands
{
    public class TextToSpeechCommand : ICommand
    {
        public string Prefix => Commands.TTS;
        public void Build(ChatMessage chatMessage, string command, string commandContent)
        {
            Voice = commandContent.GetUntilOrEmpty(":").Trim().ToLower();
            Message = commandContent[(commandContent.IndexOf(':') + 1)..];
            Username = chatMessage.Username;
            Channel = chatMessage.Channel;

        }

        public void Build(RewardRedeemed rewardRedeemed)
        {
            Voice = rewardRedeemed.Redemption.UserInput.GetUntilOrEmpty(":").Trim().ToLower();
            Message = rewardRedeemed.Redemption.UserInput[(rewardRedeemed.Redemption.UserInput.IndexOf(':') + 1)..];
            Username = rewardRedeemed.Redemption.User.DisplayName;
            Channel = rewardRedeemed.Redemption.Reward.ChannelId;
        }
        
        public string Channel { get; set; }
        public string Username { get; set; }
        public string Message { get; set; }
        public string Voice { get; set; }

    }
}
