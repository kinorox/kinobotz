using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Extensions;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

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
                response.Message = "I can't tell you that.";
                return response;
            }

            var key = $"{request.Prefix}:{request.Username.ToLower()}";

            var userLastMessage = await redisClient.Db0.GetAsync<SimplifiedChatMessage>(key);

            if (userLastMessage != null)
            {
                response.Message = string.Format(
                    "(channel: {3}, {2}) {0}: {1}.",
                    request.Username,
                    userLastMessage.Message,
                    userLastMessage.TmiSentTs.ConvertTimestampToDateTime().GetPrettyDate(),
                    userLastMessage.Channel);

                return response;
            }

            response.Message = string.Format("No messages found for the user {0}.", request.Username);
            return response;
        }
    }
}
