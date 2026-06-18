using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using twitchBot.Commands;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;

namespace twitchBot.Handlers
{
    public class GenerateRandomStreamTitleCommandHandler : BaseCommandHandler<GenerateRandomStreamTitleCommand>
    {
        private readonly IGptChatService _gptChatService;
        private readonly ILogger<GenerateRandomStreamTitleCommandHandler> _logger;
        private readonly IGptRepository _gptRepository;

        public GenerateRandomStreamTitleCommandHandler(IGptChatService gptChatService,
            IGptRepository gptRepository,
            ILogger<GenerateRandomStreamTitleCommandHandler> logger,
            IBotConnectionRepository botConnectionRepository,
            IConfiguration configuration, IAuditLogRepository auditLogRepository) : base(botConnectionRepository, configuration, auditLogRepository)
        {
            _gptChatService = gptChatService;
            _gptRepository = gptRepository;
            _logger = logger;
        }

        public override async Task<Response> InternalHandle(GenerateRandomStreamTitleCommand request, CancellationToken cancellationToken)
        {
            var behavior = await _gptRepository.GetGptBehavior(request.BotConnection.Id.ToString());

            var systemMessages = new List<string>();
            if (!string.IsNullOrEmpty(behavior))
            {
                systemMessages.Add(behavior);
            }
            systemMessages.Add("Sua resposta deve ter entre 1 e 5 palavras NO MÁXIMO. Pode usar um emoji aleatório.");

            var result = await _gptChatService.CompleteAsync(systemMessages, request.Username,
                "Me dê um título aleatório para a minha stream, sem utilizar áspas.", cancellationToken);

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
                _logger.LogError("Error during random title generation: {Error}", result.ErrorMessage);
                return new Response
                {
                    Error = true,
                    Message = "Não consegui gerar um título agora. Tente novamente."
                };
            }

            var response = result.Text ?? string.Empty;
            if (response.Length > 140)
            {
                response = response[..140];
            }

            try
            {
                await TwitchApi.Helix.Channels.ModifyChannelInformationAsync(request.BotConnection.ChannelId,
                    new ModifyChannelInformationRequest()
                    {
                        Title = response
                    });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error trying to modify the stream title.");
                return new Response
                {
                    Error = true,
                    Message = $"Error trying to modify the stream title: {e.Message}"
                };
            }

            return new Response
            {
                Error = false,
                Message = $"Title updated to: {(response.Length > 500 ? response[..500] : response)}"
            };
        }
    }
}
