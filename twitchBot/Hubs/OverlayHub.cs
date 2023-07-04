using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace twitchBot.Hubs
{
    public class OverlayHub : Hub
    {
        private readonly IHubContext<OverlayHub> hubContext;

        public OverlayHub(IHubContext<OverlayHub> hubContext)
        {
            this.hubContext = hubContext;
        }

        public async Task SendAudioStream(byte[] audioStream)
        {
            var base64String = Convert.ToBase64String(audioStream);

            await hubContext.Clients.All.SendAsync("1234", base64String);
        }
    }
}
