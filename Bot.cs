﻿using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Entities;
using twitchBot.Extensions;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace twitchBot
{
    public class Bot : IBot
    {
        readonly TwitchClient _client;
        private readonly IRedisCacheClient _redisCacheClient;

        public Bot(IConfiguration configuration, IRedisCacheClient redisCacheClient)
        {
            _redisCacheClient = redisCacheClient;
            ConnectionCredentials credentials = new ConnectionCredentials(configuration["twitch_username"], configuration["access_token"]);

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            WebSocketClient customClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(customClient);
            _client.Initialize(credentials, configuration["channel"]);

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

            _redisCacheClient.Db0.AddAsync($"lastmessage:{message.Username}", simplifiedChatMessage);
        }

        private void ExecuteCommand(ChatMessage message)
        {
            //command prefix, temp solution
            if (message.Message.StartsWith("%"))
            {
                var command = message.Message.Split(" ");

                if (command[0].Equals("%lm"))
                {
                    var userLastMessage =
                        _redisCacheClient.Db0.GetAsync<SimplifiedChatMessage>($"lastmessage:{message.Username}");

                    var result = userLastMessage.Result;

                    if (result == null)
                    {
                        _client.SendMessage(message.Channel, $"Não encontrei nenhuma mensagem do usuário {message.Username} TearGlove");
                    }
                    else
                    {
                        _client.SendMessage(message.Channel, $"A última mensagem do usuario {result.UserName} " +
                                                             $"foi '{result.Message}' " +
                                                             $"em {result.TmiSentTs.ConvertTimestampToDateTime()}' EZ");
                    }
                }

            }
        }
    }

    public interface IBot
    {
    }
}
