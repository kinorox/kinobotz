using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;
using twitchBot.Entities;
using twitchBot.Extensions;

namespace twitchBot.Handlers
{
    public class LastMessageCommandHandler : BaseCommandHandler<LastMessageCommand>
    {
        private readonly IRedisClient redisClient;

        public LastMessageCommandHandler(IRedisClient redisClient)
        {
            this.redisClient = redisClient;
        }

        public override async Task<Response> InternalHandle(LastMessageCommand request, CancellationToken cancellationToken)
        {
            var response = new Response()
            {
                Error = false
            };

            if (string.IsNullOrEmpty(request.Username))
                return null;

            if (string.Equals(request.Username.ToLower(), "kinobotz"))
            {
                response.Message = string.Format("{0}, I can't tell you that.", request.ChatMessage.Username);
                return response;
            }
            
            var userLastMessage = await redisClient.Db0.GetAsync<SimplifiedChatMessage>($"{request.Prefix}:{request.Username.ToLower()}".Trim());

            if (userLastMessage != null)
            {
                response.Message = string.Format(
                    "Last message found for {0} was '{1}' sent at {2} on the channel {3}.",
                    request.Username,
                    userLastMessage.Message,
                    userLastMessage.TmiSentTs.ConvertTimestampToDateTime(),
                    userLastMessage.Channel);

                return response;
            }

            response.Message = string.Format("No messages found for the user {0}.", request.Username);
            return response;
        }
    }
}
