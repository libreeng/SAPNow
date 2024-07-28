using Newtonsoft.Json;
using System.Collections.Generic;

namespace FSMExtension.Models
{
    public class IdaChatRequest
    {
        [JsonProperty("variables")]
        public List<KeyValuePair<string, string>> Variables { get; set; } = [];

        [JsonProperty("chatHistory")]
        public List<ChatMessage> ChatHistory { get; set; } = [];
    }
}
