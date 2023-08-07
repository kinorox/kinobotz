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
        private readonly IOpenAIAPI openAiApi;
        private readonly ILogger<GptCommandHandler> logger;
        private readonly IGptRepository gptRepository;

        public GptCommandHandler(IOpenAIAPI openAiApi, ILogger<GptCommandHandler> logger, IRedisClient redisClient, IGptRepository gptRepository) : base(redisClient)
        {
            this.openAiApi = openAiApi;
            this.logger = logger;
            this.gptRepository = gptRepository;
        }

        public override async Task<Response> InternalHandle(GptCommand request, CancellationToken cancellationToken)
        {
            string response;

            try
            {
                var chat = openAiApi.Chat.CreateConversation();

                var behavior = await gptRepository.GetGptBehavior(request.BotConnection.Id.ToString());

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
                logger.LogError(e, "Error during GPT execution.");

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
