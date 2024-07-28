using Newtonsoft.Json;
using System;

namespace FSMExtension.Models
{
    public class ChatMessage
    {
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;
    }
}
