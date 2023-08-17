using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using MediatR;
using Microsoft.Extensions.Configuration;
using twitchBot.Commands;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;

namespace twitchBot.Handlers
{
    public abstract class BaseCommandHandler<T> : IRequestHandler<T, Response> where T : BaseCommand
    {
        protected readonly ICommandRepository CommandRepository;
        protected readonly IBotConnectionRepository BotConnectionRepository;
        private readonly IConfiguration _configuration;
        protected ITwitchAPI TwitchApi;

        private readonly Dictionary<string, UserAccessLevelEnum> _userAccessLevels = new()
        {
            { "k1notv", UserAccessLevelEnum.Admin }
        };

        protected BaseCommandHandler(IBotConnectionRepository botConnectionRepository, IConfiguration configuration, ICommandRepository commandRepository)
        {
            BotConnectionRepository = botConnectionRepository;
            _configuration = configuration;
            CommandRepository = commandRepository;
        }

        public virtual int Cooldown => 0;

        public virtual bool GlobalCooldown => false;

        public abstract Task<Response> InternalHandle(T request, CancellationToken cancellationToken);

        public async Task<Response> Handle(T request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.BotConnection != null)
                {
                    var refreshedBotConnection = await BotConnectionRepository.Get(request.BotConnection.Id.ToString(), request.BotConnection.ChannelId, request.BotConnection.Login);

                    if (refreshedBotConnection != null)
                    {
                        var commandsAddedOrRemoved = false;
                        foreach (var keyValuePair in Entities.Commands.DefaultCommands)
                        {
                            var added = refreshedBotConnection.Commands.TryAdd(keyValuePair.Prefix, keyValuePair.Enabled);
                            if (added)
                            {
                                commandsAddedOrRemoved = true;
                            }
                        }

                        //remove entries from refreshBotConnection.Commands that have keys with whitespaces
                        //usefull to clean bad commands
                        var keysWithWhitespaces = refreshedBotConnection.Commands.Keys.Where(x => x.Contains(" ")).ToList();

                        foreach (var key in keysWithWhitespaces)
                        {
                            refreshedBotConnection.Commands.Remove(key);
                            commandsAddedOrRemoved = true;
                        }

                        request.BotConnection = refreshedBotConnection;

                        if(commandsAddedOrRemoved)
                            await BotConnectionRepository.SaveOrUpdate(request.BotConnection);
                    }
                }
                else
                {
                    throw new Exception("Bot connection not found.");
                }

                TwitchApi = new TwitchAPI()
                {
                    Settings =
                    {
                        ClientId = _configuration["twitch_client_id"],
                        Secret = _configuration["twitch_client_secret"],
                        AccessToken = request.BotConnection.AccessToken
                    }
                };

                if (!IsCommandEnabled(request, out var commandDisabledResponse)) return commandDisabledResponse;

                if (!UserHasAccess(request, out var accessLevel, out var accessDeniedResponse))
                    return accessDeniedResponse;

                var lastExecutionTime = GlobalCooldown
                    ? await CommandRepository.GetLastExecutionTime(request.BotConnection.Id, request.Prefix)
                    : await CommandRepository.GetLastExecutionTime(request.BotConnection.Id, request.Prefix, request.Username);

                if (lastExecutionTime.AddMinutes(Cooldown) <= DateTime.UtcNow || accessLevel == UserAccessLevelEnum.Admin)
                {
                    var response = await InternalHandle(request, cancellationToken);

                    if (!response.WasExecuted) return response;
                    
                    await CommandRepository.SetLastExecutionTime(request.BotConnection.Id, request.Prefix, request.Username, DateTime.UtcNow);
                    
                    CommandRepository.IncrementExecutionCounter(request.BotConnection.Id, request.Prefix);

                    return response;
                }

                var difference = lastExecutionTime.AddMinutes(Cooldown) - DateTime.UtcNow;

                return new Response()
                {
                    Message = $"Wait {(difference.Minutes == 0 ? difference.Seconds + "s" : difference.Minutes + "min")} to execute this command again."
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

            accessLevel = request.UserAccessLevel;

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
