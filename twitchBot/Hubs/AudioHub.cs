using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace twitchBot.Hubs
{
    public class AudioHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("receiveAudio", user, message);
        }
    }
}
