namespace Entities
{
    public class BotConnection
    {
        public Guid Id { get; set; }
        public string ChannelId { get; set; }
        public string Login { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
