﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class DisableCommandHandler : BaseCommandHandler<DisableCommand>
    {
        public DisableCommandHandler(IBotConnectionRepository botConnectionRepository, IConfiguration configuration, IAuditLogRepository auditLogRepository) : base(botConnectionRepository, configuration, auditLogRepository)
        {
        }

        public override async Task<Response> InternalHandle(DisableCommand request, CancellationToken cancellationToken)
        {
            var cleanCommand = request.Command.Trim();

            if (string.IsNullOrEmpty(cleanCommand) || string.IsNullOrWhiteSpace(cleanCommand))
            {
                return new Response()
                {
                    Message = $"Command {cleanCommand} not found. Only existing commands can be enabled (use %commands to check the existing ones)."
                };
            }

            var command = await BotConnectionRepository.GetCommand(request.BotConnection.Id, cleanCommand);

            if (command == null)
            {
                return new Response()
                {
                    Message = $"Command {cleanCommand} not found. Only existing commands can be disabled (use %commands to check the existing ones)."
                };
            }

            if (command.Prefix == request.Prefix)
            {
                return new Response()
                {
                    Message = $"Command {cleanCommand} cannot be disabled."
                };
            }

            command.Enabled = false;

            await BotConnectionRepository.SetCommand(request.BotConnection.Id, command);

            return new Response()
            {
                Message = $"Command {cleanCommand} disabled."
            };
        }
    }
}

