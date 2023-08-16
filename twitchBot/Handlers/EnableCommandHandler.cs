using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class EnableCommandHandler : BaseCommandHandler<EnableCommand>
    {
        public EnableCommandHandler(IRedisClient redisClient, IBotConnectionRepository botConnectionRepository,
            IConfiguration configuration) : base(redisClient, botConnectionRepository, configuration)
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

            if (!request.BotConnection.Commands.ContainsKey(cleanCommand))
            {
                return new Response()
                {
                    Message = $"Command {cleanCommand} not found. Only existing commands can be enabled (use %commands to check the existing ones)."
                };
            }

            request.BotConnection.Commands[cleanCommand] = true;

            await BotConnectionRepository.SaveOrUpdate(request.BotConnection);

            return new Response()
            {
                Message = $"Command {cleanCommand} enabled."
            };
        }
    }
}

