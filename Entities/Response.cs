using System;

namespace twitchBot.Entities
{
    public class Response
    {
        public string Message { get; set; }
        public bool Error { get; set; }
        public Exception Exception { get; set; }
    }
}
