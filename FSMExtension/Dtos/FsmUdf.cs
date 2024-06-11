using Newtonsoft.Json;

namespace FSMExtension.Dtos
{
    /// <summary>
    /// A custom user-defined field attached to an FSM DTO.
    /// </summary>
    public class FsmUdf
    {
        [JsonProperty("meta")]
        public string Meta { get; set; } = string.Empty;

        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
    }
}
