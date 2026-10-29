using Azure.Core;
using Azure.Identity;
using Kusto.Data;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace blz0.Authorization
{
    public class ConfigReader
    {
        private readonly IConfiguration _configuration;
        public string? TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? ClusterUri { get; set; }
        public string? KqlDatabase { get; set; }

        public ConfigReader()
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            _configuration = builder.Build();

            TenantId = _configuration["ApiCredentials:tenantId"];
            ClientId = _configuration["ApiCredentials:clientId"];
            ClientSecret = _configuration["ApiCredentials:clientSecret"];
            ClusterUri = _configuration["Kusto:clusterUri"];
            KqlDatabase = _configuration["Kusto:kqlDatabase"];
        }
    }

    public class FabricAuth
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IConfiguration _configuration;
        public HttpClient? tokenAuto;
        private string? appToken;
        private DateTimeOffset appTokenExpiresOnUtc;

        public FabricAuth(
            ProtectedLocalStorage localStorage,
            IHttpClientFactory httpClientFactory,
            ITokenAcquisition tokenAcquisition,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _tokenAcquisition = tokenAcquisition;
            _configuration = configuration;
        }

        public async Task FabricAuthAutoAsync(ClaimsPrincipal user)
        {
            var tenantId = user.FindFirst("tid")?.Value;

            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(
                new[] { "https://analysis.windows.net/powerbi/api/.default" },
                authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme,
                tenantId: tenantId,
                user: user);

            var clientAuto = _httpClientFactory.CreateClient();
            clientAuto.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            tokenAuto = clientAuto;
        }

        public async Task<HttpClient> GetFabricApiClientAsync(ClaimsPrincipal user)
        {
            var tenantId = user.FindFirst("tid")?.Value;

            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(
                new[] { "https://api.fabric.microsoft.com/Workspace.Read.All" },
                authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme,
                tenantId: tenantId,
                user: user);

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        public async Task<HttpClient> GetFabricLakehouseApiClientAsync(ClaimsPrincipal user)
        {
            var tenantId = user.FindFirst("tid")?.Value;

            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(
                new[]
                {
                    "https://api.fabric.microsoft.com/Workspace.Read.All",
                    "https://api.fabric.microsoft.com/Lakehouse.Read.All"
                },
                authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme,
                tenantId: tenantId,
                user: user);

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        public async Task<HttpClient> GetAppOnlyPowerBiClientAsync()
        {
            if (string.IsNullOrWhiteSpace(appToken) || appTokenExpiresOnUtc <= DateTimeOffset.UtcNow.AddMinutes(2))
            {
                var tenantId = _configuration["PowerBiServicePrincipal:tenantId"]
                    ?? _configuration["ApiCredentials:tenantId"];
                var clientId = _configuration["PowerBiServicePrincipal:clientId"]
                    ?? _configuration["ApiCredentials:clientId"];
                var clientSecret = _configuration["PowerBiServicePrincipal:clientSecret"]
                    ?? _configuration["ApiCredentials:clientSecret"];

                if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                {
                    throw new InvalidOperationException("Missing service principal settings for Power BI: PowerBiServicePrincipal:tenantId/clientId/clientSecret.");
                }

                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                var accessToken = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://analysis.windows.net/powerbi/api/.default" }));

                appToken = accessToken.Token;
                appTokenExpiresOnUtc = accessToken.ExpiresOn;
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", appToken);
            return client;
        }

        public class FabricAuthKQL
        {
            private readonly ConfigReader configValues = new ConfigReader();
            public KustoConnectionStringBuilder? kustoAuth;

            public async Task GetFabricAuthKQL()
            {
                if (kustoAuth != null)
                {
                    return;
                }

                var credentialKql = new ClientSecretCredential(configValues.TenantId, configValues.ClientId, configValues.ClientSecret);

                if (configValues.ClusterUri != null)
                {
                    AccessToken accessTokenKql = await credentialKql.GetTokenAsync(
                        new TokenRequestContext(new[] { configValues.ClusterUri }));

                    var tokenKql = accessTokenKql.Token;
                    kustoAuth = new KustoConnectionStringBuilder(configValues.ClusterUri, configValues.KqlDatabase)
                        .WithAadUserTokenAuthentication(tokenKql);
                }
            }
        }
    }
}
