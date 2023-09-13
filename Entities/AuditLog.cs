namespace Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public string RequestedBy { get; set; }
        public string ChatMessage { get; set; }
        public Command Command { get; set; }
        public DateTime Timestamp { get; set; }
        public string Channel { get; set; }
        public string UserAccessLevel { get; set; }
        public Response Response { get; set; }
    }
}
