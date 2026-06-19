using System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenAI;

namespace Infrastructure.Services
{
    public static class GptServiceCollectionExtensions
    {
        // Defaults to Groq (free tier) via its OpenAI-compatible endpoint. Override with config:
        //   ai_api_key  - the provider API key (Groq: console.groq.com). Falls back to OPENAI_API_KEY.
        //   ai_endpoint - OpenAI-compatible base URL (default Groq; e.g. https://api.openai.com/v1 for OpenAI).
        //   ai_model    - model id (default a Groq Llama model).
        private const string DefaultEndpoint = "https://api.groq.com/openai/v1";
        private const string DefaultModel = "llama-3.3-70b-versatile";

        /// <summary>
        /// Registers an OpenAI-compatible <see cref="IChatClient"/> (Groq by default) and the
        /// hardened <see cref="IGptChatService"/> circuit-breaker.
        /// </summary>
        public static IServiceCollection AddGptChatService(this IServiceCollection services, IConfiguration configuration)
        {
            var apiKey = configuration["ai_api_key"]
                         ?? configuration["GROQ_API_KEY"]
                         ?? configuration["OPENAI_API_KEY"]
                         ?? string.Empty;

            var endpoint = configuration["ai_endpoint"];
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = DefaultEndpoint;
            }

            var model = configuration["ai_model"] ?? configuration["openai_model"];
            if (string.IsNullOrWhiteSpace(model))
            {
                model = DefaultModel;
            }

            var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };

            IChatClient chatClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), options)
                .GetChatClient(model)
                .AsIChatClient();

            services.AddChatClient(chatClient);
            services.TryAddSingleton(TimeProvider.System);
            services.AddSingleton<IGptChatService, GptChatService>();
            return services;
        }
    }
}
