﻿using System.Threading;
using System.Threading.Tasks;
using OpenAI_API;
using OpenAI_API.Chat;
using twitchBot.Commands;
using twitchBot.Entities;

namespace twitchBot.Handlers
{
    public class GptCommandHandler : BaseCommandHandler<GptCommand>
    {
        private readonly IOpenAIAPI openAiApi;

        public GptCommandHandler(IOpenAIAPI openAiApi)
        {
            this.openAiApi = openAiApi;
        }

        public override async Task<Response> InternalHandle(GptCommand request, CancellationToken cancellationToken)
        {
            var chat = openAiApi.Chat.CreateConversation();

            chat.AppendUserInput(request.Message);
            chat.AppendUserInput("Responda em menos de 450 caracteres.");
            
            var response = await chat.GetResponseFromChatbotAsync();

            return new Response()
            {
                Error = false,
                Message = response
            };
        }
    }
}