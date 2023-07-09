using Entities;
using MediatR;
using TwitchLib.Api.Interfaces;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;
using ChatMessage = TwitchLib.Client.Models.ChatMessage;

namespace twitchBot.Commands
{
    public interface ICommand : IRequest<Response>
    {
        public string Prefix { get; }
        void Build(ChatMessage chatMessage, string command, string commandContent);
        void Build(RewardRedeemed rewardRedeemed);
        public ITwitchAPI TwitchApi { get; set; }
    }
}
