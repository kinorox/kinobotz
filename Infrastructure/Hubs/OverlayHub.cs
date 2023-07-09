using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Hubs
{
    public class OverlayHub : Hub, IOverlayHub
    {
        private readonly IHubContext<OverlayHub> hubContext;

        public OverlayHub(IHubContext<OverlayHub> hubContext)
        {
            this.hubContext = hubContext;
        }

        public async Task SendAudioStream(string id, byte[] audioStream)
        {
            var base64String = Convert.ToBase64String(audioStream);

            await hubContext.Clients.All.SendAsync(id, base64String);
        }
    }

    public interface IOverlayHub
    {
        Task SendAudioStream(string id, byte[] audioStream);
    }
}
