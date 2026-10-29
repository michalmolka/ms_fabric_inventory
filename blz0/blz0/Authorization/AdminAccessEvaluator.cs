using Azure.Identity;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace blz0.Authorization
{
    public static class AdminAccessEvaluator
    {
        public static bool IsPowerBiInventoryAdmin(ClaimsPrincipal user, IConfiguration configuration)
        {
            var adminGroupId = GetConfigValue(configuration, "Authorization:PowerBiInventoryAdminGroupId", "Authorization:PowerBIInventoryAdminGroupId");
            var adminGroupName = GetConfigValue(configuration, "Authorization:PowerBiInventoryAdminGroupName", "Authorization:PowerBIInventoryAdminGroupName") ?? "PowerBiInventoryAdmin";
            var adminRoleName = GetConfigValue(configuration, "Authorization:PowerBiInventoryAdminRole", "Authorization:PowerBIInventoryAdminRole") ?? "PowerBiInventoryAdmin";

            static bool HasClaim(ClaimsPrincipal principal, string claimType, string expectedValue) =>
                !string.IsNullOrWhiteSpace(expectedValue)
                && principal.Claims.Any(c =>
                    string.Equals(c.Type, claimType, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(c.Value, expectedValue, StringComparison.OrdinalIgnoreCase));

            return HasClaim(user, "groups", adminGroupId ?? string.Empty)
                || HasClaim(user, "groups", adminGroupName)
                || HasClaim(user, ClaimTypes.Role, adminRoleName)
                || HasClaim(user, "roles", adminRoleName);
        }

        public static async Task<bool> IsPowerBiInventoryAdminAsync(ClaimsPrincipal user, IConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (IsPowerBiInventoryAdmin(user, configuration))
            {
                return true;
            }

            var groupId = GetConfigValue(configuration, "Authorization:PowerBiInventoryAdminGroupId", "Authorization:PowerBIInventoryAdminGroupId");
            if (string.IsNullOrWhiteSpace(groupId))
            {
                return false;
            }

            var userObjectId = user.FindFirst("oid")?.Value
                ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            if (string.IsNullOrWhiteSpace(userObjectId))
            {
                return false;
            }

            foreach (var credentialSettings in GetGraphCredentialSettings(configuration))
            {
                try
                {
                    var credential = new ClientSecretCredential(
                        credentialSettings.TenantId,
                        credentialSettings.ClientId,
                        credentialSettings.ClientSecret);
                    var token = await credential.GetTokenAsync(
                        new Azure.Core.TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }),
                        cancellationToken);

                    using var http = new HttpClient();
                    http.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

                    var url = $"https://graph.microsoft.com/v1.0/users/{userObjectId}/checkMemberGroups";
                    var payload = JsonSerializer.Serialize(new { groupIds = new[] { groupId } });
                    using var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    };

                    using var response = await http.SendAsync(request, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(json);
                    if (!doc.RootElement.TryGetProperty("value", out var valueElement) || valueElement.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var item in valueElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String
                            && string.Equals(item.GetString(), groupId, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return false;
        }

        private static string? GetConfigValue(IConfiguration configuration, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = configuration[key];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static IEnumerable<(string TenantId, string ClientId, string ClientSecret)> GetGraphCredentialSettings(IConfiguration configuration)
        {
            var candidates = new[]
            {
                ("GraphCredentials", configuration["GraphCredentials:tenantId"], configuration["GraphCredentials:clientId"], configuration["GraphCredentials:clientSecret"]),
                ("ApiCredentials", configuration["ApiCredentials:tenantId"], configuration["ApiCredentials:clientId"], configuration["ApiCredentials:clientSecret"]),
                ("PowerBiServicePrincipal", configuration["PowerBiServicePrincipal:tenantId"], configuration["PowerBiServicePrincipal:clientId"], configuration["PowerBiServicePrincipal:clientSecret"])
            };

            return candidates
                .Where(e => !string.IsNullOrWhiteSpace(e.Item2)
                    && !string.IsNullOrWhiteSpace(e.Item3)
                    && !string.IsNullOrWhiteSpace(e.Item4))
                .Select(e => (e.Item2!, e.Item3!, e.Item4!))
                .Distinct();
        }
    }
}
