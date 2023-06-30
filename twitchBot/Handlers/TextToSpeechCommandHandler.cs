﻿using System;
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
using ElevenLabs.Models;
using ElevenLabs.Voices;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;
using twitchBot.Entities;

namespace twitchBot.Handlers
{
    public class TextToSpeechCommandHandler : BaseCommandHandler<TextToSpeechCommand>
    {
        private readonly ElevenLabsClient elevenLabsClient;
        private readonly IRedisClient redisClient;
        private readonly IConfiguration configuration;

        public TextToSpeechCommandHandler(ElevenLabsClient elevenLabsClient, IRedisClient redisClient, IConfiguration configuration)
        {
            this.elevenLabsClient = elevenLabsClient;
            this.redisClient = redisClient;
            this.configuration = configuration;
        }

        public override async Task<Response> InternalHandle(TextToSpeechCommand request, CancellationToken cancellationToken)
        {
            var defaultVoiceSettings = await elevenLabsClient.VoicesEndpoint.GetDefaultVoiceSettingsAsync(cancellationToken);

            defaultVoiceSettings.SimilarityBoost = 1;
            defaultVoiceSettings.Stability = 0.2f;
            
            var existingVoiceId = await redisClient.Db0.GetAsync<string>($"{request.Prefix}:{request.Voice}");

            Voice matchVoice;
            if (string.IsNullOrEmpty(existingVoiceId))
            {
                var allVoices = await elevenLabsClient.VoicesEndpoint.GetAllVoicesAsync(cancellationToken);

                if (!allVoices.Any())
                {
                    return new Response()
                    {
                        Message = "Sem vozes disponíveis."
                    };
                }

                matchVoice = allVoices.FirstOrDefault(x => string.Equals(x.Name.ToLower(), request.Voice, StringComparison.CurrentCultureIgnoreCase));

                if (matchVoice == null)
                {
                    matchVoice = allVoices.FirstOrDefault();
                }
                else
                {
                    await redisClient.Db0.AddAsync($"{request.Prefix}:{request.Voice}", matchVoice.Id);
                }
            }
            else
            {
                matchVoice = await elevenLabsClient.VoicesEndpoint.GetVoiceAsync(existingVoiceId, cancellationToken: cancellationToken);
            }

            var audioStreamingRequest = new TtsAudioStreamingRequest()
            {
                text = request.Message,
                model_id = "eleven_multilingual_v1",
                voice_settings = new Dictionary<string, string>
                {
                    {"similarity_boost", "1.0"},
                    {"stability", "1.0"}
                }
            };

            using HttpClient client = new HttpClient { BaseAddress = new Uri("https://api.elevenlabs.io/v1/") };
            
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.Add("xi-api-key", configuration["ELEVEN_LABS_API_KEY"]);

            var json = JsonConvert.SerializeObject(audioStreamingRequest);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"text-to-speech/{matchVoice.Id}/stream?optimize_streaming_latency=0", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new Response {Error = true};

            var audioStreamData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var discordWebhookClient = new DiscordWebhookClient("https://discord.com/api/webhooks/1124119346101362799/s5a7jIsyPDUR7WrykSUOdCn_U7Z8_JoaHUZfs3p79__uDDdSgcVJsDCJFe_5TdRNWCxK");
            var fileName = $"{request.Voice}-{DateTime.Now}-{(request.Message.Length > 10 ? request.Message[..10] : request.Message)}.mp3";
            await discordWebhookClient.SendFileAsync(stream: new MemoryStream(audioStreamData), fileName, $"Audio generated by {request.ChatMessage.Username} on {request.ChatMessage.Channel} at {DateTime.Now}. Audio transcription: {request.Message}");
            
            return new Response
            {
                Message = "Audio enviado no canal #ai-tts do discord."
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
