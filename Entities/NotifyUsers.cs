namespace Entities
{
    public class NotifyUsers
    {
        public NotifyUsers(List<string> usernames)
        {
            Usernames = usernames;
        }

        public List<string> Usernames { get; set; }
    }
}
