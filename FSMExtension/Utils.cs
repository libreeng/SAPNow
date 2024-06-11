using System;

namespace FSMExtension
{
    public static class Utils
    {
        public static string GetEnv(string envVariable, string? defaultValue = null)
        {
            return Environment.GetEnvironmentVariable(envVariable) ?? defaultValue ?? throw new Exception($"Missing environment variable: {envVariable}");
        }

        public static bool ExtractEmailParts(string emailAddress, out string? userName, out string? domain)
        {
            userName = null;
            domain = null;

            if (string.IsNullOrEmpty(emailAddress))
                return false;

            var index = emailAddress.IndexOf('@');
            if (index <= 0)
                return false;

            userName = emailAddress[0..index];
            domain = emailAddress[(index + 1)..];
            return true;
        }

        public static string? ExtractEmailDomain(string emailAddress)
        {
            if (!ExtractEmailParts(emailAddress, out var _, out var domain))
                return null;

            return domain;
        }
    }
}
