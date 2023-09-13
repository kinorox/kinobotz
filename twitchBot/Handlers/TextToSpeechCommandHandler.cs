using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.Webhook;
using ElevenLabs;
using ElevenLabs.Voices;
using Entities;
using Infrastructure.Repository;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class TextToSpeechCommandHandler : BaseCommandHandler<TextToSpeechCommand>
    {
        private readonly IRedisClient _redisClient;
        private readonly IConfiguration _configuration;
        private readonly IKinobotzService _kinobotzService;
        private readonly ILogger<TextToSpeechCommandHandler> _logger;

        private const int CharacterLimit = 1000;

        public TextToSpeechCommandHandler(IRedisClient redisClient, IConfiguration configuration, IKinobotzService kinobotzService, IBotConnectionRepository botConnectionRepository, ILogger<TextToSpeechCommandHandler> logger) : base(botConnectionRepository, configuration)
        {
            _redisClient = redisClient;
            _configuration = configuration;
            _kinobotzService = kinobotzService;
            _logger = logger;
        }

        public override async Task<Response> InternalHandle(TextToSpeechCommand request, CancellationToken cancellationToken)
        {
            var elevenLabsApiKey = request.BotConnection.AccessLevel >= UserAccessLevelEnum.Premium ? _configuration["ELEVEN_LABS_API_KEY"] : request.BotConnection.ElevenLabsApiKey;

            if (string.IsNullOrEmpty(elevenLabsApiKey))
            {
                return new Response()
                {
                    WasExecuted = false,
                    Message = "ElevenLabs API key is missing. Please, either configure an API key on your dashboard."
                };
            }

            //always prioritize the user's api key
            if (!string.IsNullOrEmpty(request.BotConnection.ElevenLabsApiKey))
                elevenLabsApiKey = request.BotConnection.ElevenLabsApiKey;

            var characterCount = await _redisClient.Db0.GetAsync<int?>($"{request.BotConnection.Id}:{request.Prefix}:{DateTime.UtcNow.Date}:{request.Username}:characters");

            if (characterCount > CharacterLimit)
            {
                return new Response()
                {
                    WasExecuted = false,
                    Message = $"{CharacterLimit} characters limit exceeded. Wait 24 hours to use the TTS again."
                };
            }

            var existingVoiceId = await _redisClient.Db0.GetAsync<string>($"{request.Channel}:{request.Prefix}:{request.Voice}");

            Voice matchVoice;
            var elevenLabsClient = new ElevenLabsClient(elevenLabsApiKey);
            if (string.IsNullOrEmpty(existingVoiceId))
            {
                var allVoices = await elevenLabsClient.VoicesEndpoint.GetAllVoicesAsync(cancellationToken);

                if (!allVoices.Any())
                {
                    return new Response()
                    {
                        WasExecuted = false,
                        Message = "No available voices."
                    };
                }

                matchVoice = allVoices.FirstOrDefault(x => string.Equals(x.Name.ToLower(), request.Voice, StringComparison.CurrentCultureIgnoreCase));

                if (request.RandomVoice)
                {
                    var random = new Random();
                    var index = random.Next(allVoices.Count);
                    matchVoice = allVoices[index];
                }

                if (matchVoice == null)
                {
                    return new Response()
                    {
                        WasExecuted = false,
                        Message = $"Wrong voice/syntax. Correct syntax: '<voiceName>: <message>'. Available voices: {string.Join(", ", allVoices.Where(v => string.Equals(v.Category, "cloned") || string.Equals(v.Category, "generated")).Select(v => v.Name))}."
                    };
                }

                await _redisClient.Db0.AddAsync($"{request.Channel}:{request.Prefix}:{request.Voice}", matchVoice.Id);
            }
            else
            {
                matchVoice = await elevenLabsClient.VoicesEndpoint.GetVoiceAsync(existingVoiceId, cancellationToken: cancellationToken);
            }

            var audioStreamingRequest = new TtsAudioStreamingRequest()
            {
                text = request.Message,
                model_id = "eleven_multilingual_v2",
                voice_settings = new Dictionary<string, string>
                {
                    {"similarity_boost", !string.IsNullOrEmpty(request.BotConnection.ElevenLabsSimilarityBoost) ? request.BotConnection.ElevenLabsSimilarityBoost : "1.0"},
                    {"stability", !string.IsNullOrEmpty(request.BotConnection.ElevenLabsStability) ? request.BotConnection.ElevenLabsStability : "0.3"},
                }
            };

            using HttpClient client = new HttpClient { BaseAddress = new Uri("https://api.elevenlabs.io/v1/") };
            
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.Add("xi-api-key", elevenLabsApiKey);

            var json = JsonConvert.SerializeObject(audioStreamingRequest);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"text-to-speech/{matchVoice.Id}/stream?optimize_streaming_latency=0", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new Response
                {
                    Error = true,
                    WasExecuted = false,
                    Message = "Failed generate audio. ElevenLabs has no more available characters to be used."
                };

            var audioStreamData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            if (!string.IsNullOrEmpty(request.BotConnection.DiscordTtsWebhookUrl))
            {
                try
                {
                    var discordWebhookClient = new DiscordWebhookClient(request.BotConnection.DiscordTtsWebhookUrl);
                    var fileName = $"{request.Voice}-{DateTime.Now}.mp3";
                    await discordWebhookClient.SendFileAsync(stream: new MemoryStream(audioStreamData), fileName,
                        $"Audio generated by {request.Username} on {request.Channel}'s channel at {DateTime.Now}. Audio transcription: {request.Message}");

                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error sending audio to discord webhook");
                }
            }

            //sending audio stream to signalR hub
            await _kinobotzService.SendAudioStream(request.BotConnection.Id.ToString(), audioStreamData);
            
            characterCount = characterCount.HasValue ? characterCount + request.Message.Length : request.Message.Length;

            await _redisClient.Db0.AddAsync($"{request.BotConnection.Id}:{request.Prefix}:{DateTime.UtcNow.Date}:{request.Username}:characters", characterCount, expiresIn: TimeSpan.FromHours(24));

            return new Response
            {
                Message = $"TTS successfully sent. It will play on stream shortly. Remaining daily characters: {CharacterLimit - characterCount}."
            };
        }
    }

    class TtsAudioStreamingRequest
    {
        public string text { get; set; }
        public string model_id { get; set; }
        public Dictionary<string, string> voice_settings { get; set; }
    }
}
