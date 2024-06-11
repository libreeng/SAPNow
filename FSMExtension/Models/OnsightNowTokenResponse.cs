using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace FSMExtension.Models
{
    internal class OnsightNowTokenResponse
    {
        [JsonProperty("access_token")]
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }
}
