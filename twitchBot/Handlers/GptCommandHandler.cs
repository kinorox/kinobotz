using System.Threading;
using System.Threading.Tasks;
using OpenAI_API;
using OpenAI_API.Models;
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

            chat.Model = Model.GPT4;

            chat.AppendUserInput(request.Message);
            chat.AppendUserInput("Responda em menos de 400 caracteres.");

            var response = await chat.GetResponseFromChatbotAsync();

            return new Response()
            {
                Error = false,
                Message = response.Length > 500 ? response[..500] : response
            };
        }
    }
}
