using System;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Infrastructure.Repository;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class GptCommandHandler : BaseCommandHandler<GptCommand>
    {
        private readonly IOpenAIAPI _openAiApi;
        private readonly ILogger<GptCommandHandler> _logger;
        private readonly IGptRepository _gptRepository;

        public GptCommandHandler(IOpenAIAPI openAiApi, ILogger<GptCommandHandler> logger, IRedisClient redisClient, IGptRepository gptRepository) : base(redisClient)
        {
            _openAiApi = openAiApi;
            _logger = logger;
            _gptRepository = gptRepository;
        }

        public override async Task<Response> InternalHandle(GptCommand request, CancellationToken cancellationToken)
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
                
                chat.AppendSystemMessage("Responda sempre em frases curtas, de preferência em menos de 500 caracteres.");

                chat.AppendUserInputWithName(request.Username, request.Message);

                response = await chat.GetResponseFromChatbotAsync();
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
                Message = response.Length > 500 ? response[..500] : response
            };
        }
    }
}
