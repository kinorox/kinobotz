using System;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using twitchBot.Commands;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;

namespace twitchBot.Handlers
{
    public class GenerateRandomStreamTitleCommandHandler : BaseCommandHandler<GenerateRandomStreamTitleCommand>
    {
        private readonly IOpenAIAPI _openAiApi;
        private readonly ILogger<GenerateRandomStreamTitleCommandHandler> _logger;
        private readonly IGptRepository _gptRepository;

        public GenerateRandomStreamTitleCommandHandler(IOpenAIAPI openAiApi,
            IGptRepository gptRepository,
            ILogger<GenerateRandomStreamTitleCommandHandler> logger,
            IBotConnectionRepository botConnectionRepository,
            IConfiguration configuration,
            ICommandRepository commandRepository) : base(botConnectionRepository, configuration, commandRepository)
        {
            _openAiApi = openAiApi;
            _gptRepository = gptRepository;
            _logger = logger;
        }

        public override async Task<Response> InternalHandle(GenerateRandomStreamTitleCommand request, CancellationToken cancellationToken)
        {
            string response = string.Empty;

            try
            {
                var chat = _openAiApi.Chat.CreateConversation();

                var behavior = await _gptRepository.GetGptBehavior(request.BotConnection.Id.ToString());

                if (!string.IsNullOrEmpty(behavior))
                {
                    chat.AppendSystemMessage(behavior);
                }

                chat.AppendSystemMessage("Sua resposta deve ter entre 1 e 5 palavras NO MÁXIMO. Pode usar um emoji aleatório.");

                chat.AppendUserInputWithName(request.Username, "Me dê um título aleatório para a minha stream, sem utilizar áspas.");
                
                response = await chat.GetResponseFromChatbotAsync();

                await TwitchApi.Helix.Channels.ModifyChannelInformationAsync(request.BotConnection.ChannelId,
                    new ModifyChannelInformationRequest()
                    {
                        Title = response
                    });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during GPT execution.");

                return new Response()
                {
                    Error = true,
                    Message = $"Error during GPT execution: {e.Message}"
                };
            }

            return new Response()
            {
                Error = false,
                Message = $"Title updated to: {(response.Length > 500 ? response[..500] : response)}"
            };
        }
    }
}
