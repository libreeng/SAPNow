using FSMExtension.Dtos;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace FSMExtension.Models.Fsm
{
    /// <summary>
    /// Convenience wrapper around the various FSM account details.
    /// </summary>
    public record UserDetails(
        [JsonProperty("cluster_url")] string CloudHost, 
        [JsonProperty("account_id")] int AccountId, 
        [JsonProperty("account")] string AccountName,
        [JsonProperty("companies")] List<FsmCompany> Companies,
        [JsonProperty("user_id")] string UserId,
        [JsonProperty("user_email")] string Email)
    {
        [JsonIgnore]
        public string CompanyId => (Companies.FirstOrDefault()?.Id ?? 0).ToString();
    }
}
