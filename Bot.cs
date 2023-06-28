using System;
using System.Timers;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;
using twitchBot.Entities;
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
        public static TwitchAPI TwitchApi;
        public static TwitchClient TwitchClient;
        public static TwitchPubSub TwitchPubSub;
        private readonly IRedisClient redisClient;
        private readonly IMediator mediator;
        private readonly ILogger<Bot> logger;
        private readonly IConfiguration configuration;

        public Bot(IConfiguration configuration, IRedisClient redisClient, IMediator mediator, ILogger<Bot> logger)
        {
            TwitchApi = new TwitchAPI
            {
                Settings =
                {
                    ClientId = configuration["client_id"],
                    Secret = configuration["client_secret"]
                }
            };

            TwitchApi.Settings.AccessToken = TwitchApi.Auth.GetAccessTokenAsync().Result;

            var aTimer = new Timer();
            aTimer.Elapsed += OnTimedAccessToken;
            aTimer.Interval = TimeSpan.FromMinutes(30).TotalMilliseconds;
            aTimer.Enabled = true;

            this.configuration = configuration;
            this.redisClient = redisClient;
            this.mediator = mediator;
            this.logger = logger;
            
            TwitchPubSub = new TwitchPubSub();

            TwitchPubSub.OnStreamUp += TwitchPubSubOnOnStreamUp;
            TwitchPubSub.OnStreamDown += TwitchPubSubOnOnStreamDown;
        }

        public void Connect(string channelName)
        {
            var credentials = new ConnectionCredentials(configuration["twitch_username"], configuration["access_token"]);

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            var customClient = new WebSocketClient(clientOptions);
            TwitchClient = new TwitchClient(customClient);
            TwitchClient.Initialize(credentials, channelName);

            TwitchClient.OnLog += TwitchClientOnLog;
            TwitchClient.OnConnected += TwitchClientOnConnected;
            TwitchClient.OnUserBanned += TwitchClientOnUserBanned;
            TwitchClient.OnMessageReceived += TwitchClientOnMessageReceived;

            TwitchClient.Connect();
        }

        private void OnTimedAccessToken(object sender, ElapsedEventArgs e)
        {
            TwitchApi.Settings.AccessToken = TwitchApi.Auth.GetAccessTokenAsync().Result;
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
            TwitchClient.SendMessage(e.UserBan.Channel, "xbn pepeLaugh");
        }

        private void TwitchClientOnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var pyramidMessageResponse = Pyramid.Check(e.ChatMessage);

            if(!string.IsNullOrEmpty(pyramidMessageResponse))
                TwitchClient.SendMessage(e.ChatMessage.Channel, pyramidMessageResponse);

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

            redisClient.Db0.AddAsync($"{Entities.Commands.LAST_MESSAGE}:{message.Username.ToLower()}", simplifiedChatMessage);
        }

        private async void ExecuteCommand(ChatMessage message)
        {
            try
            {
                if (!message.Message.StartsWith("%"))
                    return;

                var commandSplits = message.Message.Split(" ");

                var commandPrefix = commandSplits[0].Replace("%", string.Empty);

                ICommand command = commandPrefix switch
                {
                    Entities.Commands.LAST_MESSAGE => new LastMessageCommand() { ChatMessage = message, Username = commandSplits[1] },
                    Entities.Commands.FIRST_FOLLOW => new FirstFollowCommand() { ChatMessage = message, Username = commandSplits[1] },
                    _ => null
                };

                if (command == null)
                    return;

                var response = await mediator.Send(command);

                if (!response.Error && !string.IsNullOrEmpty(response.Message))
                {
                    SendMessageWithMe(message.Channel, response.Message);
                }
                else
                {
                    throw response.Exception;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
        }

        private void SendMessageWithMe(string channel, string message, bool dryRun = false)
        {
            TwitchClient.SendMessage(channel, $"/me {message}", dryRun);
        }
    }

    public interface IBot
    {
        void Connect(string channelName);
    }
}
