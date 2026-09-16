using System.Collections.Generic;

namespace IPOWeb.Models
{
    public class ChatAskModel
    {
        public string Message { get; set; }
    }

    public class ChatTurn
    {
        public string Role { get; set; }     // "user" | "assistant"
        public string Content { get; set; }
    }

    public class ChatAskResult
    {
        public bool Success { get; set; }
        public string Reply { get; set; }
        public bool AuthExpired { get; set; }
    }
}
