using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Webhook;
using Entities;
using Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using twitchBot.Commands;

namespace twitchBot.Handlers
{
    internal class CreateClipCommandHandler : BaseCommandHandler<CreateClipCommand>
    {
        private readonly ClipCreator _clipCreator;
        private readonly ILogger<CreateClipCommandHandler> _logger;

        public CreateClipCommandHandler(TimeProvider timeProvider, ILogger<CreateClipCommandHandler> logger, IBotConnectionRepository botConnectionRepository, IConfiguration configuration, IAuditLogRepository auditLogRepository) : base(botConnectionRepository, configuration, auditLogRepository)
        {
            _logger = logger;
            _clipCreator = new ClipCreator(timeProvider, logger);
        }

        public override async Task<Response> InternalHandle(CreateClipCommand request, CancellationToken cancellationToken)
        {
            var channel = request.BotConnection.Login;

            var clipUrl = await _clipCreator.Create(
                channel,
                async () =>
                {
                    var created = await TwitchApi.Helix.Clips.CreateClipAsync(request.BotConnection.ChannelId);
                    return created.CreatedClips.FirstOrDefault()?.Id;
                },
                async clipId =>
                {
                    var found = await TwitchApi.Helix.Clips.GetClipsAsync(new List<string> { clipId });
                    return found.Clips.FirstOrDefault()?.Url;
                },
                cancellationToken);

            if (clipUrl == null)
            {
                return new Response
                {
                    Message = "Couldn't create clip. Twitch didn't finish it, try again in a few seconds.",
                    // don't burn the cooldown on a clip that doesn't exist
                    WasExecuted = false
                };
            }

            var resultMessage = $"Clip created: {clipUrl}";

            if (!string.IsNullOrEmpty(request.BotConnection.DiscordClipsWebhookUrl))
            {
                try
                {
                    using var discordWebhookClient = new DiscordWebhookClient(request.BotConnection.DiscordClipsWebhookUrl);
                    await discordWebhookClient.SendMessageAsync(clipUrl);
                }
                catch (Exception e)
                {
                    // the clip exists; a broken webhook shouldn't hide it from chat
                    _logger.LogWarning(e, "{Channel} - couldn't post clip to Discord", channel);
                }
            }

            return new Response
            {
                Message = resultMessage
            };
        }
    }
}
