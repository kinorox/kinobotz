using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Entities;
using twitchBot.Extensions;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace twitchBot
{
    public class Bot : IBot
    {
        private static TwitchAPI _api;
        readonly TwitchClient _client;
        private readonly IRedisCacheClient _redisCacheClient;

        public Bot(IConfiguration configuration, IRedisCacheClient redisCacheClient, string channel)
        {
            _api = new TwitchAPI();
             
            _api.Settings.ClientId = configuration["client_id"];
            _api.Settings.Secret = configuration["client_secret"];
            _api.Settings.AccessToken = _api.V5.Auth.GetAccessToken();
            
            _redisCacheClient = redisCacheClient;
            ConnectionCredentials credentials = new ConnectionCredentials(configuration["twitch_username"], configuration["access_token"]);

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            WebSocketClient customClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(customClient);
            _client.Initialize(credentials, channel);

            _client.OnLog += Client_OnLog;
            _client.OnConnected += Client_OnConnected;
            _client.OnUserBanned += Client_OnUserBanned;
            _client.OnMessageReceived += Client_OnMessageReceived;

            _client.Connect();
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime}: {e.BotUsername} - {e.Data}");
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }

        private void Client_OnUserBanned(object sender, OnUserBannedArgs e)
        {
            _client.SendMessage(e.UserBan.Channel, "xbn pepeLaugh");
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var pyramidMessageResponse = Pyramid.Check(e.ChatMessage);

            if(!string.IsNullOrEmpty(pyramidMessageResponse))
                _client.SendMessage(e.ChatMessage.Channel, pyramidMessageResponse);

            StoreMessage(e.ChatMessage);

            ExecuteCommand(e.ChatMessage);

            SendCopyPasta(e);
        }

        private void SendCopyPasta(OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Message.Length >= 200)
            {
                var hashCode = e.ChatMessage.Message.GetHashCode();

                var existentHash = _redisCacheClient.Db0.GetAsync<int>($"{e.ChatMessage.Channel}:messages:{hashCode}");

                if (existentHash.Result == 0)
                {
                    _redisCacheClient.Db0.AddAsync($"{e.ChatMessage.Channel}:messages:{hashCode}", hashCode,
                        TimeSpan.FromMinutes(5));

                    SendMessageWithMe(e.ChatMessage.Channel, e.ChatMessage.Message);
                }
            }
        }

        private void StoreMessage(ChatMessage message)
        {
            var simplifiedChatMessage = new SimplifiedChatMessage()
            {
                Message = message.Message,
                TmiSentTs = message.TmiSentTs,
                Channel = message.Channel,
                UserName = message.Username,
                Id = message.Id
            };

            _redisCacheClient.Db0.AddAsync($"{message.Channel}:lastmessage:{message.Username}", simplifiedChatMessage);
        }

        private void ExecuteCommand(ChatMessage message)
        {
            //command prefix, temp solution
            if (message.Message.StartsWith("%"))
            {
                var command = message.Message.Split(" ");

                if (string.Equals(command[0], "%lm"))
                {
                    LastMessageCommand(message, command);

                    return;
                }
            }
        }

        private void LastMessageCommand(ChatMessage message, string[] command)
        {
            if (string.Equals(command[1], "kinobotz"))
            {
                _client.SendMessage(message.Channel, $"{message.Username} acha mesmo que vou te falar? B)");

                return;
            }

            var userLastMessage =
                _redisCacheClient.Db0.GetAsync<SimplifiedChatMessage>($"{message.Channel}:lastmessage:{command[1]}");

            var result = userLastMessage.Result;

            if (result == null)
            {
                SendMessageWithMe(message.Channel, $"Não encontrei nenhuma mensagem do usuário {command[1]} TearGlove");
            }
            else
            {
                SendMessageWithMe(message.Channel, $"A última mensagem do usuario {result.UserName} " +
                                                    $"foi '{result.Message}' " +
                                                    $"em {result.TmiSentTs.ConvertTimestampToDateTime()}' EZ");
            }
        }

        private void SendMessageWithMe(string channel, string message, bool dryRun = false)
        {
            _client.SendMessage(channel, $"/me {message}", dryRun);
        }
    }

    public interface IBot
    {
    }
}
