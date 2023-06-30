using System;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;
using twitchBot.Entities;
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
        private readonly TwitchClient twitchClient;
        private readonly TwitchPubSub twitchPubSub;
        private readonly IRedisClient redisClient;
        private readonly IMediator mediator;
        private readonly ILogger<Bot> logger;
        private readonly ICommandFactory commandFactory;

        public Bot(IConfiguration configuration, IRedisClient redisClient, IMediator mediator, ILogger<Bot> logger, ICommandFactory commandFactory)
        {
            this.redisClient = redisClient;
            this.mediator = mediator;
            this.logger = logger;
            this.commandFactory = commandFactory;

            twitchPubSub = new TwitchPubSub();
            
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            var customClient = new WebSocketClient(clientOptions);
            twitchClient = new TwitchClient(customClient);
            
            var credentials = new ConnectionCredentials(configuration["twitch_username"], configuration["access_token"]);
            
            twitchClient.Initialize(credentials);

            twitchClient.OnLog += TwitchClientOnLog;
            twitchClient.OnConnected += TwitchClientOnConnected;
            twitchClient.OnUserBanned += TwitchClientOnUserBanned;
            twitchClient.OnMessageReceived += TwitchClientOnMessageReceived;
            twitchPubSub.OnStreamUp += TwitchPubSubOnOnStreamUp;
            twitchPubSub.OnStreamDown += TwitchPubSubOnOnStreamDown;

            twitchClient.Connect();
        }

        public void JoinChannels(string[] channels)
        {
            logger.LogInformation($"Joining channels: {string.Join(',', channels)}");

            foreach (var channel in channels)
            {
                twitchClient.JoinChannel(channel);
            }
        }

        private void TwitchPubSubOnOnStreamDown(object? sender, OnStreamDownArgs e)
        {
            
        }

        private void TwitchPubSubOnOnStreamUp(object? sender, OnStreamUpArgs e)
        {
            
        }

        private void TwitchClientOnLog(object sender, OnLogArgs e)
        {
            logger.LogInformation($"{e.DateTime}: {e.BotUsername} - {e.Data}");
        }

        private void TwitchClientOnConnected(object sender, OnConnectedArgs e)
        {
            logger.LogInformation($"Connected to {e.AutoJoinChannel}");
        }

        private void TwitchClientOnUserBanned(object sender, OnUserBannedArgs e)
        {
            twitchClient.SendMessage(e.UserBan.Channel, "xbn pepeLaugh");
        }

        private void TwitchClientOnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var pyramidMessageResponse = Pyramid.Check(e.ChatMessage);

            if(!string.IsNullOrEmpty(pyramidMessageResponse))
                twitchClient.SendMessage(e.ChatMessage.Channel, pyramidMessageResponse);

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

            redisClient.Db0.AddAsync($"{Commands.Commands.LAST_MESSAGE}:{message.Username.ToLower()}", simplifiedChatMessage);
        }

        private async void ExecuteCommand(ChatMessage message)
        {
            try
            {
                var command = commandFactory.Build(message);

                if (command == null)
                    return;

                var response = await mediator.Send(command);
                
                twitchClient.SendReply(message.Channel, message.Id, !response.Error ? response.Message : response.Exception?.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
        }
    }

    public interface IBot
    {
        void JoinChannels(string[] channels);
    }
}
