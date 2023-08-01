using System;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Entities.Exceptions;
using Infrastructure.Repository;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class CommandCommandHandler : BaseCommandHandler<CommandCommand>
    {
        private readonly ICommandRepository _commandRepository;
        private readonly IRedisClient _redisClient;
            
        public CommandCommandHandler(ICommandRepository commandRepository, IRedisClient redisClient) : base(redisClient)
        {
            _commandRepository = commandRepository;
            _redisClient = redisClient;
        }

        public override async Task<Response> InternalHandle(CommandCommand request, CancellationToken cancellationToken)
        {
            string responseMessage;

            switch (request.Operation)
            {
                case OperationEnum.Add:
                case OperationEnum.Update:
                    await _commandRepository.CreateOrUpdate(request.BotConnection.Id, request.Name, request.Content);
                    responseMessage = $"Command {request.Name} {(request.Operation == OperationEnum.Add ? "added" : "updated")}";
                    break;
                case OperationEnum.Delete:
                    await _commandRepository.Delete(request.BotConnection.Id, request.Name);
                    responseMessage = $"Command {request.Name} deleted";
                    break;
                default:
                    throw new InvalidCommandException();
            }

            return new Response()
            {
                Message = responseMessage
            };
        }
    }
}
