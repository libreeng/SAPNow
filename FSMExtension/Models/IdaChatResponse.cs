using Newtonsoft.Json;
using System.Collections.Generic;

namespace FSMExtension.Models
{
    public class IdaChatResponse
    {
        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;

        [JsonProperty("variables")]
        public List<KeyValuePair<string, string>> Variables { get; set; } = [];
    }
}
