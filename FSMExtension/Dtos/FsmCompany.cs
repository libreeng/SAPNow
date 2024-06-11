using Newtonsoft.Json;

namespace FSMExtension.Dtos
{
    public record FsmCompany([JsonProperty("id")] int Id, [JsonProperty("name")] string Name);
}
