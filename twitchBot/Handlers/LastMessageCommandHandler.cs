using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Extensions;
using Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class LastMessageCommandHandler : BaseCommandHandler<LastMessageCommand>
    {
        private readonly IRedisClient _redisClient;

        public LastMessageCommandHandler(IRedisClient redisClient, IBotConnectionRepository botConnectionRepository, IConfiguration configuration) : base(botConnectionRepository, configuration)
        {
            _redisClient = redisClient;
        }

        public override async Task<Response> InternalHandle(LastMessageCommand request, CancellationToken cancellationToken)
        {
            var response = new Response()
            {
                Error = false
            };

            if (string.IsNullOrEmpty(request.TargetUsername))
                return null;

            if (string.Equals(request.TargetUsername.ToLower(), "kinobotz"))
            {
                response.Message = "I can't tell you that.";
                return response;
            }

            var key = $"{request.Prefix}:{request.TargetUsername.ToLower()}";

            var userLastMessage = await _redisClient.Db0.GetAsync<SimplifiedChatMessage>(key);

            if (userLastMessage != null)
            {
                response.Message = string.Format(
                    "(channel: {3}, {2}) {0}: {1}.",
                    request.TargetUsername,
                    userLastMessage.Message,
                    userLastMessage.TmiSentTs.ConvertTimestampToDateTime().GetPrettyDate(),
                    userLastMessage.Channel);

                return response;
            }

            response.Message = string.Format("No messages found for the user {0}.", request.TargetUsername);
            return response;
        }
    }
}
