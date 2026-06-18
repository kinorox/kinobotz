using System.Threading;
using System.Threading.Tasks;
using Entities;
using Entities.Exceptions;
using Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class CommandCommandHandler : BaseCommandHandler<CommandCommand>
    {
        private readonly ICustomCommandRepository _customCommandRepository;

        public CommandCommandHandler(ICustomCommandRepository customCommandRepository, IBotConnectionRepository botConnectionRepository, IConfiguration configuration, IAuditLogRepository auditLogRepository) : base(botConnectionRepository, configuration, auditLogRepository)
        {
            _customCommandRepository = customCommandRepository;
        }

        public override async Task<Response> InternalHandle(CommandCommand request, CancellationToken cancellationToken)
        {
            string responseMessage;

            switch (request.Operation)
            {
                case OperationEnum.Add:
                case OperationEnum.Update:
                    await _customCommandRepository.CreateOrUpdate(request.BotConnection.Id, request.Name, request.Content);
                    responseMessage = $"Command {request.Name} {(request.Operation == OperationEnum.Add ? "added" : "updated")}";
                    break;
                case OperationEnum.Delete:
                    await _customCommandRepository.Delete(request.BotConnection.Id, request.Name);
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
