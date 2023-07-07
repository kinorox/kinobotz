using Discord.Webhook;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using twitchBot.Commands;
using twitchBot.Entities;
using TwitchLib.Api.Helix.Models.Clips.GetClips;

namespace twitchBot.Handlers
{
    internal class CreateClipCommandHandler : BaseCommandHandler<CreateClipCommand>
    {
        private bool queryClip = true;
        
        public override async Task<Response> InternalHandle(CreateClipCommand request, CancellationToken cancellationToken)
        {
            var createClipResponse = await request.TwitchApi.Helix.Clips.CreateClipAsync(request.BotConnection.ChannelId);

            var createdClip = createClipResponse.CreatedClips.FirstOrDefault();
            
            if(string.IsNullOrEmpty(createdClip?.Id))
                return new Response()
                {
                    Message = "Couldn't create clip."
                };

            var timer = new System.Timers.Timer(15000);
            timer.Elapsed += OnTimedEvent;
            timer.Enabled = true;

            Clip resultClip = null;
            while (queryClip && resultClip == null)
            {
                var response = await request.TwitchApi.Helix.Clips.GetClipsAsync(new List<string>() { createdClip.Id });
                resultClip = response.Clips.FirstOrDefault();
            }

            if (resultClip == null)
            {
                return new Response()
                {
                    Message = "Couldn't create clip."
                };
            }
            
            var resultMessage = string.Format("Clip created: {0}", resultClip.Url);

            if (!string.IsNullOrEmpty(request.BotConnection.DiscordClipsWebhookUrl))
            {
                var discordWebhookClient = new DiscordWebhookClient(request.BotConnection.DiscordClipsWebhookUrl);

                await discordWebhookClient.SendMessageAsync(resultClip.Url);
            }
            
            return new Response()
            {
                Message = resultMessage
            };
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            queryClip = false;
        }
    }
}
