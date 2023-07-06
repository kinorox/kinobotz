using System.Threading;
using System.Threading.Tasks;
using OpenAI_API;
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
            
            var response = await chat.GetResponseFromChatbotAsync();

            return new Response()
            {
                Error = false,
                Message = response.Length > 500 ? response[..500] : response
            };
        }
    }
}
