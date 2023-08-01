using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Entities;
using Entities.Exceptions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using OnLogArgs = TwitchLib.Client.Events.OnLogArgs;
using Response = Entities.Response;

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
        private BotConnection _botConnection;

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
            twitchPubSub.OnPubSubServiceError += OnPubSubServiceError;
        }

        public Task Connect(BotConnection botConnection)
        {
            _botConnection = botConnection;

            twitchApi = new TwitchAPI
            {
                Settings =
                {
                    ClientId = configuration["client_id"],
                    Secret = configuration["client_secret"],
                    AccessToken = botConnection.AccessToken
                }
            };

            if (!string.IsNullOrEmpty(botConnection.RefreshToken))
            {
                try
                {
                    //refreshing token in case it has expired
                    var response = twitchApi.Auth.RefreshAuthTokenAsync(_botConnection.RefreshToken, configuration["client_secret"], configuration["client_id"]).Result;
                    twitchApi.Settings.AccessToken = response.AccessToken;
                    _botConnection.AccessToken = response.AccessToken;
                    _botConnection.RefreshToken = response.RefreshToken;

                    if (string.IsNullOrEmpty(_botConnection.ChannelId) || _botConnection.ChannelId == "string")
                    {
                        var user = twitchApi.Helix.Users.GetUsersAsync(logins: new List<string>() { _botConnection.Login }).Result;

                        _botConnection.ChannelId = user.Users[0].Id;
                    }

                    redisClient.Db0.AddAsync($"botconnection:{botConnection.Id}", _botConnection);

                    var aTimer = new Timer(TimeSpan.FromSeconds(response.ExpiresIn).TotalMilliseconds);
                    aTimer.Elapsed += OnOAuthTokenRefreshTimer;
                    aTimer.AutoReset = true;
                    aTimer.Enabled = true;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error when trying to refresh access token");
                }
            }

            commandFactory.Setup(twitchApi, _botConnection);

            var credentials = new ConnectionCredentials(configuration["twitch_username"], configuration["bot_access_token"]);

            twitchClient.Initialize(credentials);

            if (!string.IsNullOrEmpty(_botConnection.ChannelId))
            {
                twitchPubSub.ListenToChannelPoints(_botConnection.ChannelId);
                twitchPubSub.ListenToPredictions(_botConnection.ChannelId);
                twitchPubSub.ListenToVideoPlayback(_botConnection.ChannelId);
            }

            twitchClient.Connect();
            twitchPubSub.Connect();
            
            twitchClient.JoinChannel(_botConnection.Login);

            return Task.CompletedTask;
        }

        private void OnOAuthTokenRefreshTimer(object sender, ElapsedEventArgs e)
        {
            RefreshAccessToken();
        }

        private void OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            logger.LogError(e.Exception, "Error on PubSub");

            RefreshAccessToken();
        }

        private void OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (!e.Successful)
            {
                logger.LogError($"Couldn't connect to PubSub topic: {e.Topic}");
            }
            else
            {
                logger.LogInformation($"Successfully connected to PubSub topic: {e.Topic}");
            }
        }

        private void OnPubSubServiceConnected(object sender, EventArgs e)
        {
            twitchPubSub.SendTopics(_botConnection.AccessToken);
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
            var notifyUsers = await redisClient.Db0.GetAsync<NotifyUsers>($"{_botConnection.Id}:{Entities.Commands.NOTIFY}");

            if (notifyUsers?.Usernames == null)
                return;

            if (!notifyUsers.Usernames.Any()) return;

            var users = string.Join(", ", notifyUsers.Usernames);

            twitchClient.SendMessage(_botConnection.Login, $"{_botConnection.Login} stream ended. Notifying users: {users}");
        }

        private async void TwitchPubSubOnOnStreamUp(object sender, OnStreamUpArgs e)
        {
            if (e == null)
            {
                logger.LogInformation("OnStreamUpArgs is null");
                return;
            }

            logger.LogInformation($"stream up | channelId: {e.ChannelId} playDelay: {e.PlayDelay} serverTime: {e.ServerTime}");

            var notifyUsers = await redisClient.Db0.GetAsync<NotifyUsers>($"{_botConnection.Id}:{Entities.Commands.NOTIFY}");

            if (notifyUsers?.Usernames == null)
                return;

            if (!notifyUsers.Usernames.Any()) return;

            var users = string.Join(", ", notifyUsers.Usernames);

            twitchClient.SendMessage(_botConnection.Login, $"{_botConnection.Login} is live! BloodTrail Notifying users: {users}");

            logger.LogInformation("end notifying");
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

            redisClient.Db0.AddAsync($"{Entities.Commands.LAST_MESSAGE}:{message.Username.ToLower()}", simplifiedChatMessage);
        }

        private async void ExecuteCommand(ChatMessage message)
        {
            try
            {
                var command = commandFactory.Build(message);

                if (command == null)
                    return;

                var response = await mediator.Send(command);

                SendMessage(message, response);
            }
            catch (InvalidCommandException e)
            {
                twitchClient.SendMessage(message.Channel, e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
        }

        private void SendMessage(ChatMessage message, Response response)
        {
            switch (response.Type)
            {
                case ResponseTypeEnum.Reply:
                default:
                    twitchClient.SendReply(message.Channel, message.Id, response.Message);
                    break;
                case ResponseTypeEnum.Message:
                    twitchClient.SendMessage(message.Channel, response.Message);
                    break;
                case ResponseTypeEnum.Whisper:
                    twitchClient.SendWhisper(message.Username, response.Message);
                    break;
            }
        }

        private async void RefreshAccessToken()
        {
            try
            {
                logger.LogInformation($"{_botConnection.Login} - refreshing access token");

                _botConnection = await redisClient.Db0.GetAsync<BotConnection>($"botconnection:{_botConnection.Id}");

                var response = twitchApi.Auth.RefreshAuthTokenAsync(_botConnection.RefreshToken, configuration["client_secret"], configuration["client_id"]).Result;

                twitchApi.Settings.AccessToken = response.AccessToken;

                _botConnection.AccessToken = response.AccessToken;
                _botConnection.RefreshToken = response.RefreshToken;

                await redisClient.Db0.AddAsync($"botconnection:{_botConnection.Id}", _botConnection);

                logger.LogInformation($"{_botConnection.Login} - refreshing access token completed");
            }
            catch (Exception e)
            {
                logger.LogError(e, $"{_botConnection.Login} - Error occurred trying to refresh access token.");
            }
        }
    }

    public interface IBot
    {
        Task Connect(BotConnection botConnection);
    }
}
