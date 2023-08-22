using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;

namespace twitchBot.Handlers
{
    internal class FirstFollowCommandHandler : BaseCommandHandler<FirstFollowCommand>
    {
        private readonly IRedisClient _redisClient;

        public FirstFollowCommandHandler(IRedisClient redisClient, IBotConnectionRepository botConnectionRepository, IConfiguration configuration) : base(botConnectionRepository, configuration)
        {
            _redisClient = redisClient;
        }

        public override async Task<Response> InternalHandle(FirstFollowCommand request, CancellationToken cancellationToken)
        {
            var response = new Response()
            {
                Error = false
            };

            var userFirstFollower = await _redisClient.Db0.GetAsync<string>($"{request.Prefix}:{request.Username.ToLower()}");

            if (userFirstFollower != null)
            {
                response.Message = string.Format("The first channel {0} followed was {1}", request.Username, userFirstFollower);
                return response;
            }
            
            var getUsersResponse = TwitchApi.Helix.Users.GetUsersAsync(logins: new List<string> { request.Username.ToLower() }).Result;
            
            var user = getUsersResponse.Users.FirstOrDefault();
            
            if (user == null)
            {
                response.Message = string.Format("Invalid or inexistent user {0}", request.Username);
                return response;
            }
            
            string paginationCursor = null;
            GetUsersFollowsResponse getuserFollowsResponse = null;

            var done = false;
            while (!done)
            {
                getuserFollowsResponse = TwitchApi.Helix.Users.GetUsersFollowsAsync(fromId: user.Id, first: 100, after: paginationCursor).Result;

                if (getuserFollowsResponse != null)
                {
                    paginationCursor = getuserFollowsResponse.Pagination.Cursor;
                }

                if (paginationCursor == null)
                    done = true;
            }

            if (getuserFollowsResponse == null)
            {
                response.Message = string.Format("I couldn't find the first channel that {0} followed.", request.Username);
                return response;
            }

            var firstFollow = getuserFollowsResponse.Follows.LastOrDefault();

            if (firstFollow == null)
            {
                response.Message = string.Format("I couldn't find the first channel that {0} followed.", request.Username);
                return response;
            }

            await _redisClient.Db0.AddAsync($"{request.Prefix}:{request.Username.ToLower()}", firstFollow.ToUserName);

            response.Message = string.Format("The first channel {0} followed was {1}", request.Username, firstFollow.ToUserName);

            return response;
        }
    }
}
