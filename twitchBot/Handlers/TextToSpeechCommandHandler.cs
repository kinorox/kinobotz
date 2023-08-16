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
using ElevenLabs.Voices;
using Entities;
using Infrastructure.Repository;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    public class TextToSpeechCommandHandler : BaseCommandHandler<TextToSpeechCommand>
    {
        private readonly ElevenLabsClient _elevenLabsClient;
        private readonly IRedisClient _redisClient;
        private readonly IConfiguration _configuration;
        private readonly IKinobotzService _kinobotzService;

        public TextToSpeechCommandHandler(ElevenLabsClient elevenLabsClient, IRedisClient redisClient, IConfiguration configuration, IKinobotzService kinobotzService, IBotConnectionRepository botConnectionRepository) : base(redisClient, botConnectionRepository, configuration)
        {
            _elevenLabsClient = elevenLabsClient;
            _redisClient = redisClient;
            _configuration = configuration;
            _kinobotzService = kinobotzService;
        }

        public override int Cooldown => 5;
        public override bool GlobalCooldown => false;

        public override async Task<Response> InternalHandle(TextToSpeechCommand request, CancellationToken cancellationToken)
        {
            var characterCount = await _redisClient.Db0.GetAsync<int?>($"{request.BotConnection.Id}:{request.Prefix}:{DateTime.UtcNow.Date}:{request.Username}:characters");

            if (characterCount > 3000)
            {
                return new Response()
                {
                    WasExecuted = false,
                    Message = "3000 characters limit exceeded. Wait 24 hours to use the TTS again."
                };
            }

            var existingVoiceId = await _redisClient.Db0.GetAsync<string>($"{request.Prefix}:{request.Voice}");

            Voice matchVoice;
            if (string.IsNullOrEmpty(existingVoiceId))
            {
                var allVoices = await _elevenLabsClient.VoicesEndpoint.GetAllVoicesAsync(cancellationToken);

                if (!allVoices.Any())
                {
                    return new Response()
                    {
                        WasExecuted = false,
                        Message = "No available voices."
                    };
                }

                matchVoice = allVoices.FirstOrDefault(x => string.Equals(x.Name.ToLower(), request.Voice, StringComparison.CurrentCultureIgnoreCase));

                if (matchVoice == null)
                {
                    return new Response()
                    {
                        WasExecuted = false,
                        Message = $"Wrong voice/syntax. Correct syntax: '<voiceName>: <message>'. Available voices: {string.Join(", ", allVoices.Where(v => string.Equals(v.Category, "cloned")).Select(v => v.Name))}."
                    };
                }

                await _redisClient.Db0.AddAsync($"{request.Prefix}:{request.Voice}", matchVoice.Id);
            }
            else
            {
                matchVoice = await _elevenLabsClient.VoicesEndpoint.GetVoiceAsync(existingVoiceId, cancellationToken: cancellationToken);
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
            client.DefaultRequestHeaders.Add("xi-api-key", _configuration["ELEVEN_LABS_API_KEY"]);

            var json = JsonConvert.SerializeObject(audioStreamingRequest);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"text-to-speech/{matchVoice.Id}/stream?optimize_streaming_latency=0", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new Response {Error = true};

            var audioStreamData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            if (!string.IsNullOrEmpty(request.BotConnection.DiscordTtsWebhookUrl))
            {
                var discordWebhookClient = new DiscordWebhookClient(request.BotConnection.DiscordTtsWebhookUrl);
                var fileName = $"{request.Voice}-{DateTime.Now}-{(request.Message.Length > 10 ? request.Message[..10] : request.Message)}.mp3";
                await discordWebhookClient.SendFileAsync(stream: new MemoryStream(audioStreamData), fileName, $"Audio generated by {request.Username} on {request.Channel}'s channel at {DateTime.Now}. Audio transcription: {request.Message}");
            }

            //sending audio stream to signalR hub
            await _kinobotzService.SendAudioStream(request.BotConnection.Id.ToString(), audioStreamData);
            
            characterCount = characterCount.HasValue ? characterCount + request.Message.Length : request.Message.Length;

            await _redisClient.Db0.AddAsync($"{request.BotConnection.Id}:{request.Prefix}:{DateTime.UtcNow.Date}:{request.Username}:characters", characterCount, expiresIn: TimeSpan.FromHours(24));

            return new Response
            {
                Message = $"TTS successfully sent. It will play on stream shortly. Remaining daily characters: {3000 - characterCount}."
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
