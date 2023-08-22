using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class NotifyCommandHandler : BaseCommandHandler<NotifyCommand>
    {
        private readonly IRedisClient _redisClient;

        public NotifyCommandHandler(IRedisClient redisClient, IBotConnectionRepository botConnectionRepository, IConfiguration configuration) : base(botConnectionRepository, configuration)
        {
            _redisClient = redisClient;
        }

        public override async Task<Response> InternalHandle(NotifyCommand request, CancellationToken cancellationToken)
        {
            var notifyUsers = await _redisClient.Db0.GetAsync<NotifyUsers>($"{request.BotConnection.Id}:{request.Prefix}") ?? new NotifyUsers(new List<string>());
            
            if (notifyUsers.Usernames.Contains(request.Username))
            {
                return new Response()
                {
                    Message = "You are already on the notification list.",
                };
            }

            notifyUsers.Usernames.Add(request.Username);

            await _redisClient.Db0.AddAsync($"{request.BotConnection.Id}:{request.Prefix}", notifyUsers);

            return new Response()
            {
                Message = "You will be notified when the streamer goes live."
            };
        }
    }
}
