using FSMExtension.Models;
using FSMExtension.Models.Fsm;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FSMExtension.Services
{
    public interface IAuthService
    {
        Task<(UserDetails?, string?)> GetAppTokenAsync(string cloudHost, string accountName, string userId, string fsmAuthToken);
        Task<UserDetails?> GetUserAsync(string? authorizationHeader);
    }

    public class AuthService : IAuthService
    {
        private const string CheckTokenUriFormat = "https://{0}/api/oauth2/v1/check_token";

        private readonly Dictionary<UserDetails, string> _loggedInUsers = new();
        private readonly Dictionary<string, UserDetails> _activeTokens = new();


        public async Task<(UserDetails?, string?)> GetAppTokenAsync(string cloudHost, string accountName, string userId, string fsmAuthToken)
        {
            // Sanity check: given accountName must match the env variable
            var expectedAccountName = Utils.GetEnv("SAP_FSM_ACCOUNT_NAME");
            if (!string.Equals(accountName, expectedAccountName, StringComparison.OrdinalIgnoreCase))
                return default;

            var clientCredentials = ClientCredentials.GetFsm(cloudHost, accountName);

            // Check the incoming SAP FSM auth token using SAP's oauth2 service
            var uri = string.Format(CheckTokenUriFormat, cloudHost);
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", clientCredentials.ToBasicAuthString());
            httpClient.DefaultRequestHeaders.Add("X-Client-ID", "login-with-token");
            httpClient.DefaultRequestHeaders.Add("X-Client-Version", "1.0.0");
            var body = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("token", fsmAuthToken)
            ]);

            var response = await httpClient.PostAsync(uri, body);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<UserDetails>(json);

                // Make sure the userId we get back matches what we expect
                if (user != null && string.Equals(user.UserId, userId))
                {
                    if (!_loggedInUsers.TryGetValue(user, out var appToken))
                    {
                        appToken = Guid.NewGuid().ToString();
                        _loggedInUsers.Add(user, appToken);
                        _activeTokens.Add(appToken, user);
                    }

                    return (user, appToken);
                }
            }

            return default;
        }

        public Task<UserDetails?> GetUserAsync(string? authorizationHeader)
        {
            if (authorizationHeader == null || !authorizationHeader.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult((UserDetails?)null);

            var appToken = authorizationHeader[7..];
            _activeTokens.TryGetValue(appToken, out var user);
            return Task.FromResult(user);
        }
    }
}
