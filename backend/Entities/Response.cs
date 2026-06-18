namespace Entities
{
    public class Response
    {
        public ResponseTypeEnum Type { get; set; }
        public string Message { get; set; }
        public bool Error { get; set; }
        public bool WasExecuted { get; set; } = true;
        public Exception Exception { get; set; }
    }

    public enum ResponseTypeEnum
    {
        Reply,
        Message,
        Whisper
    }
}
