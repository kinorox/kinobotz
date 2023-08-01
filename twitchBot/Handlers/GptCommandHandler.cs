using System;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class GptCommandHandler : BaseCommandHandler<GptCommand>
    {
        private readonly IOpenAIAPI openAiApi;
        private readonly IRedisClient redisClient;
        private readonly ILogger<GptCommandHandler> logger;

        public GptCommandHandler(IOpenAIAPI openAiApi, ILogger<GptCommandHandler> logger, IRedisClient redisClient) : base(redisClient)
        {
            this.openAiApi = openAiApi;
            this.logger = logger;
            this.redisClient = redisClient;
        }

        public override async Task<Response> InternalHandle(GptCommand request, CancellationToken cancellationToken)
        {
            string response;

            try
            {
                var chat = openAiApi.Chat.CreateConversation();

                var behavior = await redisClient.Db0.GetAsync<string>($"{request.BotConnection.Id}:{Entities.Commands.GPT_BEHAVIOR}");

                if (!string.IsNullOrEmpty(behavior))
                {
                    chat.AppendSystemMessage(behavior);
                }

                chat.AppendUserInput(request.Message);

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
