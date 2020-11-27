using System;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Entities;
using twitchBot.Factories;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using OnLogArgs = TwitchLib.Client.Events.OnLogArgs;

namespace twitchBot
{
    public class Bot : IBot
    {
        public static TwitchAPI Api;
        public static TwitchClient Client;
        public static TwitchPubSub PubSubClient;
        private readonly IRedisCacheClient _redisCacheClient;

        public Bot(IConfiguration configuration, IRedisCacheClient redisCacheClient, string channel)
        {
            Api = new TwitchAPI();
             
            Api.Settings.ClientId = configuration["client_id"];
            Api.Settings.Secret = configuration["client_secret"];
            Api.Settings.AccessToken = Api.V5.Auth.GetAccessToken();
            
            _redisCacheClient = redisCacheClient;
            ConnectionCredentials credentials = new ConnectionCredentials(configuration["twitch_username"], configuration["access_token"]);

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            WebSocketClient customClient = new WebSocketClient(clientOptions);
            Client = new TwitchClient(customClient);
            Client.Initialize(credentials, channel);

            Client.OnLog += Client_OnLog;
            Client.OnConnected += Client_OnConnected;
            Client.OnUserBanned += Client_OnUserBanned;
            Client.OnMessageReceived += Client_OnMessageReceived;

            Client.Connect();

            PubSubClient = new TwitchPubSub();

            PubSubClient.OnStreamUp += PubSubClient_OnOnStreamUp;
            PubSubClient.OnStreamDown += PubSubClient_OnOnStreamDown;
        }

        private void PubSubClient_OnOnStreamDown(object? sender, OnStreamDownArgs e)
        {
            
        }

        private void PubSubClient_OnOnStreamUp(object? sender, OnStreamUpArgs e)
        {
            
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
            Client.SendMessage(e.UserBan.Channel, "xbn pepeLaugh");
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var pyramidMessageResponse = Pyramid.Check(e.ChatMessage);

            if(!string.IsNullOrEmpty(pyramidMessageResponse))
                Client.SendMessage(e.ChatMessage.Channel, pyramidMessageResponse);

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

            _redisCacheClient.Db0.AddAsync($"{message.Channel}:lastmessage:{message.Username.ToLower()}", simplifiedChatMessage);
        }

        private void ExecuteCommand(ChatMessage message)
        {
            if (!message.Message.StartsWith("%"))
                return;

            var splittedCommand = message.Message.Split(" ");

            var commandFactory = new CommandFactory(_redisCacheClient);

            var command = commandFactory.Build(splittedCommand[0]);
            
            var responseMessage = command.Execute(message, splittedCommand[1]);

            if(!string.IsNullOrEmpty(responseMessage))
                SendMessageWithMe(message.Channel, responseMessage);
        }

        private void SendMessageWithMe(string channel, string message, bool dryRun = false)
        {
            Client.SendMessage(channel, $"/me {message}", dryRun);
        }
    }

    public interface IBot
    {
    }
}
