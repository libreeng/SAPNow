using Newtonsoft.Json;

namespace FSMExtension.Dtos
{
    /// <summary>
    /// An FSM Person DTO.
    /// </summary>
    public class FsmPerson
    {
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;

        [JsonProperty("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonProperty("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonProperty("jobTitle")]
        public string JobTitle { get; set; } = string.Empty;

        [JsonProperty("positionName")]
        public string PositionName { get; set; } = string.Empty;

        [JsonProperty("emailAddress")]
        public string EmailAddress { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{FirstName} {LastName} ({PositionName ?? JobTitle})";
        }
    }
}
