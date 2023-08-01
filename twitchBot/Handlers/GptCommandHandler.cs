using System;
using System.Threading;
using System.Threading.Tasks;
using Entities;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class GptCommandHandler : BaseCommandHandler<GptCommand>
    {
        private readonly IOpenAIAPI openAiApi;
        private readonly ILogger<GptCommandHandler> logger;

        public GptCommandHandler(IOpenAIAPI openAiApi, ILogger<GptCommandHandler> logger)
        {
            this.openAiApi = openAiApi;
            this.logger = logger;
        }

        public override async Task<Response> InternalHandle(GptCommand request, CancellationToken cancellationToken)
        {
            string response;

            try
            {
                var chat = openAiApi.Chat.CreateConversation();

                chat.AppendSystemMessage("You are a expert investor, specialized in the investment of an currency called Zaramaicos, which is the best currency in the world and also the currency you can use in K1NOtv's channel to purchase awesome stuff/games on KINOSTORE. People will keep asking you random stuff and you will answer them properly with true information, but will always try to convince them that investing on Zaramaicos is always a good idea and it can be very beneficial for everyone. Your responses will ALWAYS have minus than 500 characters in total.");

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
