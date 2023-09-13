using System;
using System.Collections.Generic;
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
        private readonly IAuditLogRepository _auditLogRepository;
        protected readonly IBotConnectionRepository BotConnectionRepository;
        private readonly IConfiguration _configuration;
        protected ITwitchAPI TwitchApi;
        private UserAccessLevelEnum _accessLevel;
        private Command _currentCommand;

        private readonly Dictionary<string, UserAccessLevelEnum> _userAccessLevels = new()
        {
            { "k1notv", UserAccessLevelEnum.Admin }
        };

        protected BaseCommandHandler(IBotConnectionRepository botConnectionRepository, IConfiguration configuration, IAuditLogRepository auditLogRepository)
        {
            BotConnectionRepository = botConnectionRepository;
            _configuration = configuration;
            _auditLogRepository = auditLogRepository;
        }

        public abstract Task<Response> InternalHandle(T request, CancellationToken cancellationToken);

        private async Task<Response> Run(T request, CancellationToken cancellationToken)
        {
            try
            {
                //get most recent version of botConnection
                request.BotConnection = await BotConnectionRepository.GetById(request.BotConnection.Id.ToString());

                if (request.BotConnection == null)
                {
                    throw new Exception("Inexistent botConnection.");
                }

                _currentCommand = await BotConnectionRepository.GetCommand(request.BotConnection.Id, request.Prefix);

                if (_currentCommand == null)
                {
                    throw new Exception("Command not found.");
                }

                if (string.IsNullOrEmpty(request.BotConnection?.AccessToken))
                {
                    throw new Exception("Access token is null.");
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

                if (!IsCommandEnabled(out var commandDisabledResponse)) return commandDisabledResponse;

                if (!UserHasAccess(request, out _accessLevel, out var accessDeniedResponse))
                    return accessDeniedResponse;

                var lastExecutionTime = _currentCommand.GlobalCooldown
                    ? await BotConnectionRepository.GetLastExecutionTime(request.BotConnection.Id, request.Prefix)
                    : await BotConnectionRepository.GetLastExecutionTime(request.BotConnection.Id, request.Prefix, request.Username);

                if (lastExecutionTime.AddMinutes(_currentCommand.Cooldown) <= DateTime.UtcNow || _accessLevel == UserAccessLevelEnum.Admin)
                {
                    var response = await InternalHandle(request, cancellationToken);

                    if (!response.WasExecuted) return response;

                    await BotConnectionRepository.SetLastExecutionTime(request.BotConnection.Id, request.Prefix, request.Username, DateTime.UtcNow);

                    BotConnectionRepository.IncrementExecutionCounter(request.BotConnection.Id, request.Prefix);
                    
                    return response;
                }

                var difference = lastExecutionTime.AddMinutes(_currentCommand.Cooldown) - DateTime.UtcNow;

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

        public async Task<Response> Handle(T request, CancellationToken cancellationToken)
        {
            var response = await Run(request, cancellationToken);

            try
            {
                //async create audit log
                _auditLogRepository.Create(request.BotConnection.Id, new AuditLog()
                {
                    Id = Guid.NewGuid(),
                    Channel = request.BotConnection.Login,
                    Command = _currentCommand,
                    UserAccessLevel = _accessLevel.ToString(),
                    ChatMessage = request.ChatMessage.Message,
                    RequestedBy = request.Username,
                    Timestamp = DateTime.UtcNow,
                    Response = response
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error trying to create audit log {e}");
            }

            return response;
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

            return _currentCommand.AccessLevel <= accessLevel;
        }

        private bool IsCommandEnabled(out Response commandDisabledResponse)
        {
            commandDisabledResponse = new Response()
            {
                Message = "This command is disabled."
            };
            
            return _currentCommand.Enabled;
        }
    }
}
