using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Entities;
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
using Timer = System.Timers.Timer;

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
        private readonly IConfiguration configuration;
        private TwitchAPI twitchApi;
        private BotConnection botConnection;

        public Bot(IConfiguration configuration, IRedisClient redisClient, IMediator mediator, ILogger<Bot> logger, ICommandFactory commandFactory)
        {
            this.configuration = configuration;
            this.redisClient = redisClient;
            this.mediator = mediator;
            this.logger = logger;
            this.commandFactory = commandFactory;
            
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            var customClient = new WebSocketClient(clientOptions);
            twitchClient = new TwitchClient(customClient);
            twitchClient.OnLog += TwitchClientOnLog;
            twitchClient.OnConnected += TwitchClientOnConnected;
            twitchClient.OnUserBanned += TwitchClientOnUserBanned;
            twitchClient.OnMessageReceived += TwitchClientOnMessageReceived;

            twitchPubSub = new TwitchPubSub();
            twitchPubSub.OnStreamUp += TwitchPubSubOnOnStreamUp;
            twitchPubSub.OnStreamDown += TwitchPubSubOnOnStreamDown;
            twitchPubSub.OnChannelPointsRewardRedeemed += TwitchPubSubChannelPoints;
            twitchPubSub.OnPubSubServiceConnected += OnPubSubServiceConnected;
            twitchPubSub.OnListenResponse += OnListenResponse;
            twitchPubSub.OnPrediction += TwitchPubSubOnOnPrediction;
        }

        private async void OnTimedAccessToken(object sender, ElapsedEventArgs e)
        {
            var response = await twitchApi.Auth.RefreshAuthTokenAsync(botConnection.RefreshToken, configuration["client_secret"], configuration["client_id"]);
            twitchApi.Settings.AccessToken = response.AccessToken;
        }

        public void Connect(BotConnection botConnection)
        {
            this.botConnection = botConnection;

            twitchApi = new TwitchAPI
            {
                Settings =
                {
                    ClientId = configuration["client_id"],
                    Secret = configuration["client_secret"],
                    AccessToken = botConnection.AccessToken
                }
            };

            commandFactory.Setup(twitchApi);

            var credentials = new ConnectionCredentials(configuration["twitch_username"], configuration["bot_access_token"]);

            twitchClient.Initialize(credentials);

            var user = twitchApi.Helix.Users.GetUsersAsync(logins: new List<string>() { botConnection.Login }).Result;

            var userId = user.Users.FirstOrDefault()?.Id;

            twitchPubSub.ListenToChannelPoints(userId);
            twitchPubSub.ListenToPredictions(userId);

            twitchClient.Connect();
            twitchPubSub.Connect();
            twitchClient.JoinChannel(botConnection.Login);

            var aTimer = new Timer();
            aTimer.Elapsed += OnTimedAccessToken;
            aTimer.Interval = TimeSpan.FromMinutes(60).TotalMilliseconds;
            aTimer.Enabled = true;
        }

        private void TwitchPubSubOnOnPrediction(object sender, OnPredictionArgs e)
        {
            
        }

        private void OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (!e.Successful)
            {
                logger.LogError($"Couldn't connect to PubSub topic: {e.Topic}");
            }
        }

        private void OnPubSubServiceConnected(object sender, EventArgs e)
        {
            twitchPubSub.SendTopics(configuration["access_token"]);
        }

        private async void TwitchPubSubChannelPoints(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            var ttsCommand = commandFactory.Build(e.RewardRedeemed);

            if (ttsCommand == null)
                return;

            var response = await mediator.Send(ttsCommand);

            logger.LogInformation(response.Message);
        }

        private async void TwitchPubSubOnOnStreamDown(object sender, OnStreamDownArgs e)
        {
            var notifyUsers = await redisClient.Db0.GetAsync<NotifyUsers>($"{Commands.Commands.NOTIFY}");

            if (notifyUsers?.Usernames == null)
                return;

            if (!notifyUsers.Usernames.Any()) return;

            var users = string.Join(", ", notifyUsers.Usernames);

            twitchClient.SendMessage("k1notv", $"K1NOtv stream ended. Notify users: {users}");
        }

        private async void TwitchPubSubOnOnStreamUp(object sender, OnStreamUpArgs e)
        {
            var notifyUsers = await redisClient.Db0.GetAsync<NotifyUsers>($"{Commands.Commands.NOTIFY}");

            if (notifyUsers?.Usernames == null)
                return;

            if (!notifyUsers.Usernames.Any()) return;

            var users = string.Join(", ", notifyUsers.Usernames);

            twitchClient.SendMessage("k1notv", $"K1NOtv is live. Notify users: {users}");
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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping bot");
            return Task.CompletedTask;
        }
    }

    public interface IBot
    {
        void Connect(BotConnection botConnection);
    }
}
