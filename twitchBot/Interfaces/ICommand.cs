using TwitchLib.Client.Models;

namespace twitchBot.Interfaces
{
    public interface ICommand
    {
        public string Name { get; }
        string Execute(ChatMessage message, string command);
    }
}
