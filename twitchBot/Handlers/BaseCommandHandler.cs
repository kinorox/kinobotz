using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using MediatR;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public abstract class BaseCommandHandler<T> : IRequestHandler<T, Response> where T : BaseCommand
    {
        private readonly IRedisClient _redisClient;

        private readonly Dictionary<string, UserAccessLevelEnum> _userAccessLevels = new()
        {
            { "k1notv", UserAccessLevelEnum.Admin }
        };

        protected BaseCommandHandler(IRedisClient redisClient)
        {
            _redisClient = redisClient;
        }

        public virtual int Cooldown => 0;

        public virtual bool GlobalCooldown => false;

        public abstract Task<Response> InternalHandle(T request, CancellationToken cancellationToken);

        public async Task<Response> Handle(T request, CancellationToken cancellationToken)
        {
            try
            {
                if (!IsCommandEnabled(request, out var commandDisabledResponse)) return commandDisabledResponse;

                if (!UserHasAccess(request, out var accessLevel, out var accessDeniedResponse))
                    return accessDeniedResponse;

                var lastExecutionTime = GlobalCooldown
                    ? await _redisClient.Db0.GetAsync<DateTime>(
                        $"{request.BotConnection.Id}:{request.Prefix}:lastexecution")
                    : await _redisClient.Db0.GetAsync<DateTime>(
                        $"{request.BotConnection.Id}:{request.Prefix}:lastexecution:{request.Username}");

                if (lastExecutionTime.AddMinutes(Cooldown) <= DateTime.UtcNow ||
                    accessLevel == UserAccessLevelEnum.Admin)
                {
                    var response = await InternalHandle(request, cancellationToken);

                    if (!response.WasExecuted) return response;

                    var timeNow = DateTime.UtcNow;

                    await _redisClient.Db0.AddAsync($"{request.BotConnection.Id}:{request.Prefix}:lastexecution",
                        timeNow);
                    await _redisClient.Db0.AddAsync(
                        $"{request.BotConnection.Id}:{request.Prefix}:lastexecution:{request.Username}", timeNow);

                    return response;
                }

                var difference = lastExecutionTime.AddMinutes(Cooldown) - DateTime.UtcNow;

                return new Response()
                {
                    Message =
                        $"Wait {(difference.Minutes == 0 ? difference.Seconds + "s" : difference.Minutes + "min")} to execute this command again."
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private bool UserHasAccess(T request, out UserAccessLevelEnum accessLevel, out Response accessDeniedResponse)
        {
            accessDeniedResponse = new Response()
            {
                Message = "You don't have access to this command."
            };

            accessLevel = UserAccessLevelEnum.Default;

            if (_userAccessLevels.ContainsKey(request.Username.ToLower()))
            {
                accessLevel = _userAccessLevels[request.Username.ToLower()];
            }
            else if (string.Equals(request.Username.ToLower(), request.BotConnection.Login))
            {
                accessLevel = UserAccessLevelEnum.Broadcaster;
            }

            if (request.AccessLevels.Contains(accessLevel)) return true;
            {
                return false;
            }
        }

        private static bool IsCommandEnabled(T request, out Response commandDisabledResponse)
        {
            commandDisabledResponse = new Response()
            {
                Message = "This command is disabled."
            };

            if (!request.BotConnection.Commands.TryGetValue(request.Prefix, out var commandEnabled)) return false;

            if (commandEnabled) return true;
            {
                return false;
            }
        }
    }
}
