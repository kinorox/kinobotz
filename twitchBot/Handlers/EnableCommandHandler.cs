using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class EnableCommandHandler : BaseCommandHandler<EnableCommand>
    {
        public EnableCommandHandler(IBotConnectionRepository botConnectionRepository, IConfiguration configuration) : base(botConnectionRepository, configuration)
        {
        }

        public override async Task<Response> InternalHandle(EnableCommand request, CancellationToken cancellationToken)
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
                    Message = $"Command {cleanCommand} cannot be enabled."
                };
            }

            command.Enabled = true;
            
            await BotConnectionRepository.SetCommand(request.BotConnection.Id, command);

            return new Response()
            {
                Message = $"Command {cleanCommand} enabled."
            };
        }
    }
}

