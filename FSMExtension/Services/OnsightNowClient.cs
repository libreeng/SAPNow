using FSMExtension.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FSMExtension.Services
{
    public class OnsightNowClient(ClientCredentials clientCredentials)
    {
        /// <summary>
        /// OAuth scopes required to use the Onsight NOW Meetings API.
        /// </summary>
        private const string ApiScopes = "meeting_api collection_api ida_api";


        /// <summary>
        /// The Onsight NOW API Token endpoint. This can be overridden at runtime by the now_OnsightNowTokenEndpoint environment variable.
        /// </summary>
        public string TokenEndpoint { get; set; } = "https://demo-login.onsightnow.com/connect/token";

        /// <summary>
        /// The Onsight NOW Meetings API endpoint. This can be overridden at runtime by the now_OnsightNowMeetingsEndpoint environment variable.
        /// </summary>
        public string MeetingsEndpoint { get; set; } = "https://demo-api.onsightnow.com/meetings";

        /// <summary>
        /// The Onsight NOW Ida Chat API endpoint. This can be overridden at runtime by the now_OnsightNowMeetingsEndpoint environment variable.
        /// </summary>
        public string IdaChatEndpoint { get; set; } = "https://demo-api.onsightnow.com/ida";

        /// <summary>
        /// Creates an Onsight NOW Meeting.
        /// </summary>
        /// <param name="meetingRequest"></param>
        /// <returns></returns>
        public async Task<string> ScheduleMeetingAsync(MeetingRequest meetingRequest)
        {
            var accessToken = await GetAccessTokenAsync();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
            
            using var response = await client.PostAsJsonAsync(MeetingsEndpoint, meetingRequest);
            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var meetingResponse = await response.Content.ReadAsAsync<CreateMeetingResponse>();
            return meetingResponse.JoinUrl;
        }

        public async Task<IdaChatResponse> ChatWithIdaAsync(IdaChatRequest chatRequest)
        {
            var accessToken = await GetAccessTokenAsync();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

            using var response = await client.PostAsJsonAsync(IdaChatEndpoint, chatRequest);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IdaChatResponse>();
        }

        /// <summary>
        /// Helper which gets an Onsight Now API Token using the client_credentials flow.
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetAccessTokenAsync()
        {
            var values = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", ApiScopes),
                new KeyValuePair<string, string>("client_id", clientCredentials.ClientId),
                new KeyValuePair<string, string>("client_secret", clientCredentials.ClientSecret)
            ]);

            using var client = new HttpClient();
            using var response = await client.PostAsync(TokenEndpoint, values);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<OnsightNowTokenResponse>();
            return tokenResponse?.AccessToken ?? string.Empty;
        }
    }
}
