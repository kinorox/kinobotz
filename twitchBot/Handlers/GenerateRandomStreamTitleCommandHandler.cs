using System;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;

namespace twitchBot.Handlers
{
    public class GenerateRandomStreamTitleCommandHandler : BaseCommandHandler<GenerateRandomStreamTitleCommand>
    {
        private readonly IOpenAIAPI _openAiApi;
        private readonly ILogger<GenerateRandomStreamTitleCommandHandler> _logger;
        private readonly IGptRepository _gptRepository;

        public GenerateRandomStreamTitleCommandHandler(IRedisClient redisClient,
            IOpenAIAPI openAiApi,
            IGptRepository gptRepository,
            ILogger<GenerateRandomStreamTitleCommandHandler> logger, IBotConnectionRepository botConnectionRepository) : base(redisClient, botConnectionRepository)
        {
            _openAiApi = openAiApi;
            _gptRepository = gptRepository;
            _logger = logger;
        }

        public override async Task<Response> InternalHandle(GenerateRandomStreamTitleCommand request, CancellationToken cancellationToken)
        {
            string response;

            try
            {
                var chat = _openAiApi.Chat.CreateConversation();

                var behavior = await _gptRepository.GetGptBehavior(request.BotConnection.Id.ToString());

                if (!string.IsNullOrEmpty(behavior))
                {
                    chat.AppendSystemMessage(behavior);
                }

                chat.AppendSystemMessage("Sua resposta deve ter entre 1 e 10 palavras, além de 1 emoji aleatório.");

                chat.AppendUserInputWithName(request.Username, "Me dê um título aleatório para a minha stream, sem utilizar áspas.");

                response = await chat.GetResponseFromChatbotAsync();

                await request.TwitchApi.Helix.Channels.ModifyChannelInformationAsync(request.BotConnection.ChannelId,
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
