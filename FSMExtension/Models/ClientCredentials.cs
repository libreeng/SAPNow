using System.Text;
using System;

namespace FSMExtension.Models
{
    public record ClientCredentials(string ClientId, string ClientSecret)
    {
        public string ToBasicAuthString()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}"));
        }

        public static ClientCredentials GetFsm(string cloudHost, string accountName)
        {
            // Read from environment variables. In a multi-tenant scenario, this could be fetched
            // from a database, keyed by the cloudHost + accountName args.
            return new ClientCredentials(Utils.GetEnv("SAP_FSM_CLIENT_ID"), Utils.GetEnv("SAP_FSM_CLIENT_SECRET"));
        }

        public static ClientCredentials GetOnsightNow(string cloudHost, string accountName)
        {
            // Read from environment variables. In a multi-tenant scenario, this could be fetched
            // from a database, keyed by the cloudHost + accountName args.
            return new ClientCredentials(Utils.GetEnv("ONSIGHT_NOW_CLIENT_ID"), Utils.GetEnv("ONSIGHT_NOW_CLIENT_SECRET"));
        }
    }
}
