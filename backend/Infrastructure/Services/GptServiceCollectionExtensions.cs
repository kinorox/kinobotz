using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenAI;

namespace Infrastructure.Services
{
    public static class GptServiceCollectionExtensions
    {
        /// <summary>
        /// Registers an OpenAI-backed <see cref="IChatClient"/> and the hardened
        /// <see cref="IGptChatService"/>. Reads "OPENAI_API_KEY" and an optional
        /// "openai_model" (defaults to a current, cost-effective model).
        /// </summary>
        public static IServiceCollection AddGptChatService(this IServiceCollection services, IConfiguration configuration)
        {
            var apiKey = configuration["OPENAI_API_KEY"] ?? string.Empty;
            var model = configuration["openai_model"];
            if (string.IsNullOrWhiteSpace(model))
            {
                model = "gpt-4o-mini";
            }

            IChatClient chatClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey))
                .GetChatClient(model)
                .AsIChatClient();

            services.AddChatClient(chatClient);
            services.TryAddSingleton(TimeProvider.System);
            services.AddSingleton<IGptChatService, GptChatService>();
            return services;
        }
    }
}
