using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace twitchBot.Hubs
{
    public class AudioHub : Hub
    {
        private readonly IHubContext<AudioHub> hubContext;

        public AudioHub(IHubContext<AudioHub> hubContext)
        {
            this.hubContext = hubContext;
        }

        public async Task SendAudioStream(byte[] audioStream)
        {
            var base64String = Convert.ToBase64String(audioStream);

            await hubContext.Clients.All.SendAsync("receiveAudio", base64String);
        }
    }
}
