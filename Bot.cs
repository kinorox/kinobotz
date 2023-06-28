using System;
using System.Timers;
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
        private readonly IRedisCacheClient redisCacheClient;

        public Bot(IConfiguration configuration, IRedisCacheClient redisCacheClient, string channel)
        {
            Api = new TwitchAPI
            {
                Settings =
                {
                    ClientId = configuration["client_id"],
                    Secret = configuration["client_secret"]
                }
            };

            Api.Settings.AccessToken = Api.V5.Auth.GetAccessToken();

            var aTimer = new Timer();
            aTimer.Elapsed += OnTimedAccessToken;
            aTimer.Interval = TimeSpan.FromMinutes(30).TotalMilliseconds;
            aTimer.Enabled = true;

            this.redisCacheClient = redisCacheClient;
            var credentials = new ConnectionCredentials(configuration["twitch_username"], configuration["access_token"]);

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            var customClient = new WebSocketClient(clientOptions);
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

        private void OnTimedAccessToken(object sender, ElapsedEventArgs e)
        {
            Api.Settings.AccessToken = Api.V5.Auth.GetAccessToken();
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

            redisCacheClient.Db0.AddAsync($"{message.Channel}:lastmessage:{message.Username.ToLower()}", simplifiedChatMessage);
        }

        private void ExecuteCommand(ChatMessage message)
        {
            if (!message.Message.StartsWith("%"))
                return;

            var splittedCommand = message.Message.Split(" ");

            var commandFactory = new CommandFactory(redisCacheClient);

            var command = commandFactory.Build(splittedCommand[0]);

            if (command == null)
                return;

            string responseMessage = null;
            try
            {
                responseMessage = command.Execute(message, splittedCommand[1]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

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
