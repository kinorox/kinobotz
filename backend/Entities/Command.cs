namespace Entities
{
    public class Command
    {
        public string Prefix { get; set; }
        public UserAccessLevelEnum AccessLevel { get; set; } = UserAccessLevelEnum.Default;
        public int Cooldown { get; set; } = 0;
        public bool GlobalCooldown { get; set; } = false;
        public string Description { get; set; }
        public bool Enabled { get; set; }
    }
}
