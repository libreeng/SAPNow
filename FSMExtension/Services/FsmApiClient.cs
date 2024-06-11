using FSMExtension.Dtos;
using FSMExtension.Models;
using FSMExtension.Models.Fsm;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace FSMExtension.Services
{
    /// <summary>
    /// Provides access to the FSM API's data model objects.
    /// </summary>
    public interface IFsmApiClient
    {
        Task<FsmActivity?> GetActivityAsync(UserDetails fromUser, ClientCredentials clientCreds, string activityId);
        Task<FsmContact?> GetContactAsync(UserDetails fromUser, ClientCredentials clientCreds, string contactId);
        Task<FsmEquipment?> GetEquipmentAsync(UserDetails fromUser, ClientCredentials clientCreds, string equipmentId);
        Task<FsmPerson[]> GetPersonsAsync(UserDetails fromUser, ClientCredentials clientCreds, params string[] personIds);
    }

    /// <summary>
    /// Implementation of the IFsmDataService.
    /// </summary>
    public class FsmApiClient : IFsmApiClient
    {
        public const string ClientVersion = "1.0";


        /// <summary>
        /// URL for getting tokens to be used with FSM APIs.
        /// </summary>
        private static readonly string TokenUrl = "https://auth.coresuite.com/api/oauth2/v1/token";
        private static readonly FormUrlEncodedContent TokenRequestBody = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        // These were the latest versions of each data model at the time of writing.
        // Older versions could likely be used as well, since only a small number of
        // fields from each are actually used by this extension.
        private static readonly Dictionary<string, int> DtoVersions = new()
        {
            { "Activity", 37 },
            { "Contact", 17 },
            { "Person", 24 },
            { "Equipment", 23 },
            { "Attachment", 18 },
        };

        private readonly MemoryCache cache;


        public FsmApiClient(HttpClient httpClient, IConfiguration config, ILogger<FsmApiClient> logger)
        {
            HttpClient = httpClient;
            Logger = logger;

            var cacheOptions = new MemoryCacheOptions();
            cache = new MemoryCache(cacheOptions);

            // Get DTO versions to use by reading from appsettings.json
            var dtoVersions = config.GetSection("FSM:DTOs");
            foreach (var dtoVersion in dtoVersions.AsEnumerable())
            {
                if (int.TryParse(dtoVersion.Value, out int version))
                    DtoVersions[dtoVersion.Key] = version;
            }
        }

        private HttpClient HttpClient { get; }

        private ILogger<FsmApiClient> Logger { get; }

        public async Task<FsmActivity?> GetActivityAsync(UserDetails fromUser, ClientCredentials clientCreds, string activityId)
        {
            var request = await CreateMessageAsync(fromUser.CloudHost, fromUser.AccountId, fromUser.CompanyId, clientCreds, ClientVersion, "Activity", activityId, DtoVersions["Activity"]);
            return await GetDtoAsync<FsmActivity>(request, "Activity");
        }

        public async Task<FsmContact?> GetContactAsync(UserDetails fromUser, ClientCredentials clientCreds, string contactId)
        {
            var request = await CreateMessageAsync(fromUser.CloudHost, fromUser.AccountId, fromUser.CompanyId, clientCreds, ClientVersion, "Contact", contactId, DtoVersions["Contact"]);
            return await GetDtoAsync<FsmContact>(request, "Contact");
        }

        public async Task<FsmEquipment?> GetEquipmentAsync(UserDetails fromUser, ClientCredentials clientCreds, string equipmentId)
        {
            var customRemoteExpertMapping = new Customization.UdfNames
            {
                Name = Utils.GetEnv("SAP_FSM_REMOTE_EXPERT_NAME_FIELD", "OnsightRemoteExpertName"),
                Email = Utils.GetEnv("SAP_FSM_REMOTE_EXPERT_EMAIL_FIELD", "OnsightRemoteExpertEmail")
            };

            var queryApiRequest = new Uri($"{fromUser.CloudHost}/api/query/v1?dtos=Equipment.{DtoVersions["Equipment"]}");
            var body = JsonContent.Create(new { query = $"SELECT eqp.id, eqp.code, eqp.udf.{customRemoteExpertMapping.Email}, eqp.udf.{customRemoteExpertMapping.Name} FROM Equipment eqp WHERE eqp.id = '{equipmentId}'" });
            var message = await CreateMessageAsync(HttpMethod.Post, queryApiRequest, fromUser.CloudHost, fromUser.AccountId, fromUser.CompanyId, clientCreds, ClientVersion, body);

            var eqpResult = await GetDtoAsync<FsmEquipmentResult>(message, "eqp");
            if (eqpResult == null || eqpResult.UdfValues == null || eqpResult.UdfValues.Count == 0)
                return null;

            // Map each user-defined field by its name
            var udfMap = eqpResult.UdfValues.ToDictionary(udf => udf.Name);

            var expertEmail = string.Empty;
            var expertName = string.Empty;
            if (customRemoteExpertMapping.Name != "meta" && customRemoteExpertMapping.Email != "meta")
            {
                Logger.LogInformation($"Found a customRemoteExpertMapping. Name Field={customRemoteExpertMapping.Name}; Email Field={customRemoteExpertMapping.Email}");
                expertName = udfMap.GetValueOrDefault(customRemoteExpertMapping.Name)?.Value ?? string.Empty;
                expertEmail = udfMap.GetValueOrDefault(customRemoteExpertMapping.Email)?.Value ?? string.Empty;
            }

            return new FsmEquipment
            {
                Code = eqpResult.Code,

                // Our FSM custom fields are NOT of type FsmContact (FSM UI isn't user-friendly in listing Contacts for user),
                // so we need to instantiate our own FsmContact from the individual custom fields we do have.
                RemoteExpert = new FsmContact
                {
                    FirstName = expertName,
                    LastName = string.Empty,
                    EmailAddress = expertEmail,
                    Code = string.Empty,
                    PositionName = string.Empty
                }
            };
        }

        public async Task<FsmPerson[]> GetPersonsAsync(UserDetails fromUser, ClientCredentials clientCreds, params string[] personIds)
        {
            var tasks = new Task<FsmPerson?>[personIds.Length];

            for (var i = 0; i < personIds.Length; i++)
            {
                var personId = personIds[i];
                var request = await CreateMessageAsync(fromUser.CloudHost, fromUser.AccountId, fromUser.CompanyId, clientCreds, ClientVersion, "Person", personId, DtoVersions["Person"]);
                tasks[i] = GetDtoAsync<FsmPerson>(request, "Person");
            }

            var persons = await Task.WhenAll(tasks);
            if (persons == null)
                return [];

            return persons.Where(p => p != null)
                .Cast<FsmPerson>()
                .ToArray();
        }

        private async Task<T?> GetDtoAsync<T>(HttpRequestMessage request, string dtoName)
            where T : class
        {
            var response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return default;

            var content = await response.Content.ReadAsStringAsync();
            var jobject = JObject.Parse(content);
            var itemArray = jobject["data"] as JArray;
            if (itemArray == null || itemArray.Count == 0)
                return default;

            var firstItem = itemArray.First();
            var itemObj = firstItem[dtoName.ToLower()];

            if (itemObj == null)
                return default;

            return itemObj.ToObject<T>();
        }

        private Task<HttpRequestMessage> CreateMessageAsync(
            string cloudHost,
            int accountId,
            string companyId,
            ClientCredentials clientCreds,
            string clientVersion,
            string dtoName,
            string dtoId,
            int dtoVersion)
        {
            var requestUri = new Uri($"{cloudHost}/api/data/v4/{dtoName}/{dtoId}?dtos={dtoName}.{dtoVersion}");
            return CreateMessageAsync(HttpMethod.Get, requestUri, cloudHost, accountId, companyId, clientCreds, clientVersion);
        }

        private async Task<HttpRequestMessage> CreateMessageAsync(
            HttpMethod method,
            Uri requestUri,
            string cloudHost,
            int accountId,
            string companyId,
            ClientCredentials clientCreds,
            string clientVersion,
            HttpContent? body = null)
        {
            var message = new HttpRequestMessage(method, requestUri);

            var token = await GenerateTokenAsync(accountId, companyId, cloudHost, clientCreds);
            message.Headers.Authorization = AuthenticationHeaderValue.Parse($"Bearer {token}");
            message.Headers.Add("X-Client-ID", clientCreds.ClientId);
            message.Headers.Add("X-Client-Version", clientVersion);
            message.Headers.Add("X-Account-ID", accountId.ToString());
            message.Headers.Add("X-Company-ID", companyId);
            message.Content = body;

            return message;
        }

        private async Task<string?> GenerateTokenAsync(int fsmAccountId, string fsmCompanyId, string cloudHost, ClientCredentials clientCreds)
        {
            // If we already have an in-memory cached token, return it
            var cacheKey = $"{fsmAccountId}:{fsmCompanyId}:{cloudHost}";
            if (cache.TryGetValue(cacheKey, out string? token))
                return token;

            // Otherwise, we need to request a new token from FSM's OAuth2 provider
            var message = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
            var authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientCreds.ClientId}:{clientCreds.ClientSecret}"));

            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            message.Content = TokenRequestBody;
            var startTime = DateTime.UtcNow;
            var response = await HttpClient.SendAsync(message);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var jobject = JObject.Parse(responseString);
            token = jobject["access_token"].Value<string>();
            var expiration = startTime.AddSeconds(jobject["expires_in"].Value<long>());

            // Before returning, cache this token in memory for next time
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSize(1)
                .SetAbsoluteExpiration(expiration);

            cache.Set(cacheKey, token, cacheEntryOptions);
            return token;
        }
    }
}
