using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Entities;
using Entities.Exceptions;
using Infrastructure.Repository;
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
using OnLogArgs = TwitchLib.Client.Events.OnLogArgs;
using Response = Entities.Response;

namespace twitchBot
{
    public class Bot : IBot
    {
        private readonly TwitchClient _twitchClient;
        private readonly IRedisClient _redisClient;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly ILogger<Bot> _logger;
        private readonly ICommandFactory _commandFactory;
        private readonly IConfiguration _configuration;
        private TwitchAPI _twitchApi;
        private BotConnection _botConnection;
        private readonly IBotConnectionRepository _botConnectionRepository;

        public Bot(IConfiguration configuration, IRedisClient redisClient, ICommandDispatcher commandDispatcher, ILogger<Bot> logger, ICommandFactory commandFactory, IBotConnectionRepository botConnectionRepository)
        {
            _configuration = configuration;
            _redisClient = redisClient;
            _commandDispatcher = commandDispatcher;
            _logger = logger;
            _commandFactory = commandFactory;
            _botConnectionRepository = botConnectionRepository;

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            var customClient = new WebSocketClient(clientOptions);
            _twitchClient = new TwitchClient(customClient);
            _twitchClient.OnLog += TwitchClientOnLog;
            _twitchClient.OnConnected += TwitchClientOnConnected;
            _twitchClient.OnUserBanned += TwitchClientOnUserBanned;
            _twitchClient.OnMessageReceived += TwitchClientOnMessageReceived;
        }

        public string ChannelId => _botConnection?.ChannelId;

        // ── EventSub-driven handlers (replace the former Twitch PubSub events) ──
        // Invoked by EventSubRouter when a notification for this channel arrives.

        public Task HandleSubscriptionAsync(int months, string subMessage)
        {
            try
            {
                if (_botConnection is not { UseTtsOnSubscription: true }) return Task.CompletedTask;
                if (months < _botConnection.TtsMinimumResubMonthsAmount) return Task.CompletedTask;

                var command = new TextToSpeechCommand(_botConnection)
                {
                    Channel = _botConnection.Login,
                    Message = subMessage,
                    Voice = !string.IsNullOrEmpty(_botConnection.ElevenLabsDefaultVoice) ? _botConnection.ElevenLabsDefaultVoice : null,
                    RandomVoice = string.IsNullOrEmpty(_botConnection.ElevenLabsDefaultVoice),
                    Username = "k1notv"
                };

                return _commandDispatcher.Send(command);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error during subscription event");
                return Task.CompletedTask;
            }
        }

        public Task HandleBitsAsync(int bits, string chatMessage)
        {
            try
            {
                if (_botConnection is not { UseTtsOnBits: true }) return Task.CompletedTask;
                if (bits < _botConnection.TtsMinimumBitAmount) return Task.CompletedTask;

                // strip the "CheerNNN" tokens from the message
                var result = Regex.Replace(chatMessage ?? string.Empty, @"Cheer\d+", string.Empty);

                var command = new TextToSpeechCommand(_botConnection)
                {
                    Channel = _botConnection.Login,
                    Message = result,
                    Voice = !string.IsNullOrEmpty(_botConnection.ElevenLabsDefaultVoice) ? _botConnection.ElevenLabsDefaultVoice : null,
                    RandomVoice = string.IsNullOrEmpty(_botConnection.ElevenLabsDefaultVoice),
                    Username = "k1notv"
                };

                return _commandDispatcher.Send(command);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error during bits event");
                return Task.CompletedTask;
            }
        }

        public async Task HandleStreamUpAsync()
        {
            var notifyUsers = await _redisClient.Db0.GetAsync<NotifyUsers>($"{_botConnection.Id}:{Entities.Commands.NOTIFY}");

            if (notifyUsers?.Usernames == null || !notifyUsers.Usernames.Any()) return;

            var users = string.Join(", ", notifyUsers.Usernames);

            _twitchClient.SendMessage(_botConnection.Login, $"{_botConnection.Login} is live! BloodTrail Notifying users: {users}");
        }

        public async Task HandleStreamDownAsync()
        {
            var notifyUsers = await _redisClient.Db0.GetAsync<NotifyUsers>($"{_botConnection.Id}:{Entities.Commands.NOTIFY}");

            if (notifyUsers?.Usernames == null || !notifyUsers.Usernames.Any()) return;

            var users = string.Join(", ", notifyUsers.Usernames);

            _twitchClient.SendMessage(_botConnection.Login, $"{_botConnection.Login} stream ended. Notifying users: {users}");
        }

        public Task Connect(BotConnection botConnection)
        {
            _botConnection = botConnection;

            _twitchApi = new TwitchAPI
            {
                Settings =
                {
                    ClientId = _configuration["twitch_client_id"],
                    Secret = _configuration["twitch_client_secret"],
                    AccessToken = botConnection.AccessToken
                }
            };

            if (!string.IsNullOrEmpty(botConnection.RefreshToken))
            {
                try
                {
                    //refreshing token in case it has expired
                    var response = _twitchApi.Auth.RefreshAuthTokenAsync(_botConnection.RefreshToken, _configuration["twitch_client_secret"], _configuration["twitch_client_id"]).Result;
                    _twitchApi.Settings.AccessToken = response.AccessToken;
                    _botConnection.AccessToken = response.AccessToken;
                    _botConnection.RefreshToken = response.RefreshToken;

                    if (string.IsNullOrEmpty(_botConnection.ChannelId) || _botConnection.ChannelId == "string")
                    {
                        var user = _twitchApi.Helix.Users.GetUsersAsync(logins: new List<string>() { _botConnection.Login }).Result;

                        _botConnection.ChannelId = user.Users[0].Id;
                    }

                    _botConnectionRepository.SaveOrUpdate(_botConnection);

                    var aTimer = new Timer(TimeSpan.FromSeconds(response.ExpiresIn).TotalMilliseconds);
                    aTimer.Elapsed += OnOAuthTokenRefreshTimer;
                    aTimer.AutoReset = true;
                    aTimer.Enabled = true;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error when trying to refresh access token");
                }
            }

            _commandFactory.Setup(_twitchApi, _botConnection);

            var credentials = new ConnectionCredentials(_configuration["twitch_username"], _configuration["bot_access_token"]);

            _twitchClient.Initialize(credentials);
            _twitchClient.Connect();
            _twitchClient.JoinChannel(_botConnection.Login);

            return Task.CompletedTask;
        }

        private void OnOAuthTokenRefreshTimer(object sender, ElapsedEventArgs e)
        {
            _ = RefreshAccessToken();
        }

        private void TwitchClientOnLog(object sender, OnLogArgs e)
        {
            _logger.LogInformation($"{e.DateTime}: {e.BotUsername} - {e.Data}");
        }

        private void TwitchClientOnConnected(object sender, OnConnectedArgs e)
        {
            _logger.LogInformation($"Connected to {e.AutoJoinChannel}");
        }

        private void TwitchClientOnUserBanned(object sender, OnUserBannedArgs e)
        {

        }

        private void TwitchClientOnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var pyramidMessageResponse = Pyramid.Check(e.ChatMessage);

            if(!string.IsNullOrEmpty(pyramidMessageResponse))
                _twitchClient.SendMessage(e.ChatMessage.Channel, pyramidMessageResponse);

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

            _redisClient.Db0.AddAsync($"{Entities.Commands.LAST_MESSAGE}:{message.Username.ToLower()}", simplifiedChatMessage);
        }

        private async void ExecuteCommand(ChatMessage message)
        {
            try
            {
                var command = _commandFactory.Build(message);

                if (command == null)
                    return;

                var response = await _commandDispatcher.Send(command);

                if (!string.IsNullOrEmpty(response.Message))
                {
                    SendMessage(message, response);
                }
            }
            catch (InvalidCommandException e)
            {
                _twitchClient.SendMessage(message.Channel, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private void SendMessage(ChatMessage message, Response response)
        {
            switch (response.Type)
            {
                case ResponseTypeEnum.Reply:
                default:
                    _twitchClient.SendReply(message.Channel, message.Id, response.Message);
                    break;
                case ResponseTypeEnum.Message:
                    _twitchClient.SendMessage(message.Channel, response.Message);
                    break;
                case ResponseTypeEnum.Whisper:
                    _twitchClient.SendWhisper(message.Username, response.Message);
                    break;
            }
        }

        private async Task RefreshAccessToken()
        {
            try
            {
                _logger.LogInformation($"{_botConnection.Login} - refreshing access token");

                _botConnection = await _botConnectionRepository.GetById(_botConnection.Id.ToString());

                var response = await _twitchApi.Auth.RefreshAuthTokenAsync(_botConnection.RefreshToken, _configuration["twitch_client_secret"], _configuration["twitch_client_id"]);

                _twitchApi.Settings.AccessToken = response.AccessToken;

                _botConnection.AccessToken = response.AccessToken;
                _botConnection.RefreshToken = response.RefreshToken;

                await _botConnectionRepository.SaveOrUpdate(_botConnection);

                _logger.LogInformation($"{_botConnection.Login} - refreshing access token completed");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{_botConnection.Login} - Error occurred trying to refresh access token.");
            }
        }
    }

    public interface IBot
    {
        string ChannelId { get; }
        Task Connect(BotConnection botConnection);
        Task HandleBitsAsync(int bits, string chatMessage);
        Task HandleSubscriptionAsync(int months, string subMessage);
        Task HandleStreamUpAsync();
        Task HandleStreamDownAsync();
    }
}
