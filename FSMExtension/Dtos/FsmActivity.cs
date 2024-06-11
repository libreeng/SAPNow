using Newtonsoft.Json;

namespace FSMExtension.Dtos
{
    /// <summary>
    /// An FSM Activity DTO.
    /// </summary>
    public class FsmActivity
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;

        [JsonProperty("equipment")]
        public string EquipmentId { get; set; } = string.Empty;

        [JsonProperty("contact")]
        public string Contact { get; set; } = string.Empty;

        [JsonProperty("responsibles")]
        public string[] Responsibles { get; set; } = [];

        public override string ToString()
        {
            return $"Activity {Code}";
        }
    }
}
