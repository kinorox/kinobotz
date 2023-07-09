using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class NotifyCommandHandler : BaseCommandHandler<NotifyCommand>
    {
        private readonly IRedisClient redisClient;

        public NotifyCommandHandler(IRedisClient redisClient)
        {
            this.redisClient = redisClient;
        }

        public override async Task<Response> InternalHandle(NotifyCommand request, CancellationToken cancellationToken)
        {
            var notifyUsers = await redisClient.Db0.GetAsync<NotifyUsers>($"{request.BotConnection.Id}:{request.Prefix}") ?? new NotifyUsers();

            notifyUsers.Usernames ??= new List<string>();

            if (notifyUsers.Usernames.Contains(request.Username))
            {
                return new Response()
                {
                    Message = "You are already on the notification list.",
                };
            }

            notifyUsers.Usernames.Add(request.Username);

            await redisClient.Db0.AddAsync($"{request.BotConnection.Id}:{request.Prefix}", notifyUsers);

            return new Response()
            {
                Message = "You will be notified when the streamer goes live."
            };
        }
    }
}
