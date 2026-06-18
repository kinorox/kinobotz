using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class GptCommandHandler : BaseCommandHandler<GptCommand>
    {
        private readonly IGptChatService _gptChatService;
        private readonly ILogger<GptCommandHandler> _logger;
        private readonly IGptRepository _gptRepository;

        public GptCommandHandler(IGptChatService gptChatService, ILogger<GptCommandHandler> logger, IGptRepository gptRepository, IBotConnectionRepository botConnectionRepository, IConfiguration configuration, IAuditLogRepository auditLogRepository) : base(botConnectionRepository, configuration, auditLogRepository)
        {
            _gptChatService = gptChatService;
            _logger = logger;
            _gptRepository = gptRepository;
        }

        public override async Task<Response> InternalHandle(GptCommand request, CancellationToken cancellationToken)
        {
            var behavior = await _gptRepository.GetGptBehavior(request.BotConnection.Id.ToString());

            var systemMessages = new List<string>();
            if (!string.IsNullOrEmpty(behavior))
            {
                systemMessages.Add(behavior);
            }
            systemMessages.Add("Responda sempre em frases curtas, de preferência em menos de 500 caracteres.");

            var result = await _gptChatService.CompleteAsync(systemMessages, request.Username, request.Message, cancellationToken);

            if (result.Unavailable)
            {
                return new Response
                {
                    Error = true,
                    Message = "O GPT está temporariamente indisponível. Tente novamente mais tarde."
                };
            }

            if (result.ErrorMessage != null)
            {
                _logger.LogError("Error during GPT execution: {Error}", result.ErrorMessage);
                return new Response
                {
                    Error = true,
                    Message = "Não consegui gerar uma resposta agora. Tente novamente."
                };
            }

            var response = result.Text ?? string.Empty;
            return new Response
            {
                Error = false,
                Message = response.Length > 500 ? response[..500] : response
            };
        }
    }
}
