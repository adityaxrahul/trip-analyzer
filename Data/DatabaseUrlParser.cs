using System;
using System.Text.RegularExpressions;

namespace TripAnalyzer.Data
{
    public static class DatabaseUrlParser
    {
        public static string ConvertConnectionString(string? databaseUrl)
        {
            if (string.IsNullOrWhiteSpace(databaseUrl))
            {
                // S-18 fix: Fail loudly rather than silently using weak default credentials.
                // Set the DATABASE_URL environment variable to configure the database connection.
                throw new InvalidOperationException(
                    "DATABASE_URL is not configured. Set the DATABASE_URL environment variable to a valid PostgreSQL connection string or URL.");
            }

            // If it's already a standard Npgsql key-value connection string (contains '='), return as-is
            if (databaseUrl.Contains("=") && !databaseUrl.StartsWith("postgres://") && !databaseUrl.StartsWith("postgresql://"))
            {
                return databaseUrl;
            }

            try
            {
                var uri = new Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':');
                var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
                var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');

                var connString = $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
                return connString;
            }
            catch
            {
                // Fallback attempt with Regex in case Uri parsing fails
                var match = Regex.Match(databaseUrl, @"postgres(?:ql)?://([^:]+):([^@]+)@([^:/]+)(?::(\d+))?/(.+)");
                if (match.Success)
                {
                    var user = match.Groups[1].Value;
                    var password = match.Groups[2].Value;
                    var host = match.Groups[3].Value;
                    var port = string.IsNullOrEmpty(match.Groups[4].Value) ? "5432" : match.Groups[4].Value;
                    var database = match.Groups[5].Value.Split('?')[0];

                    return $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
                }

                return databaseUrl;
            }
        }
    }
}
