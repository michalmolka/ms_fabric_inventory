using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace blz0.Operations;

public class AiKqlService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AiKqlService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string> GenerateKqlAsync(string prompt)
    {
        var baseUrl = _configuration["LmStudio:BaseUrl"] ?? "http://localhost:1234/v1";
        var model = _configuration["LmStudio:Model"] ?? "gpt-oss-20b";
        var apiKey = _configuration["LmStudio:ApiKey"] ?? "lm-studio";

        var systemPrompt = """
You generate KQL only.
Return one valid KQL query, no markdown, no explanation.
Use this pattern:
['ms-fabric-logs']
| take <limit>
| project Id=id, EventTime=['time'], EventType=['type'], ItemId=data['itemId'], ItemName=data['itemName'], WorkspaceId=data['workspaceId'], WorkspaceName=data['workspaceName'], ItemKind=data['itemKind']
| filter EventTime >= ago(<timewindow>)

Allowed columns:
- id, time, type, source, subject, ingestion_time(), data.*
- data.itemId, data.itemName, data.workspaceId, data.workspaceName, data.itemKind, data.executingPrincipalId, data.executingPrincipalType

Microsoft Fabric Event Record (KQL Database):
- id: unique event id
- datacontenttype: MIME type of payload
- specversion: CloudEvents version
- source: identifier of the Fabric resource that generated the event
- time: when the event occurred
- subject: resource path of affected Fabric object
- dataschemaversion: version of the event schema
- type: event type (operation and outcome)
- data.itemId: Fabric item id
- data.itemName: Fabric item name
- data.workspaceId: workspace id
- data.workspaceName: workspace name
- data.itemKind: item type (Notebook, Pipeline, Lakehouse, Semantic Model, Report, etc.)
- data.executingPrincipalId: id of user/service principal/managed identity
- data.executingPrincipalType: type of executing principal
- IngestionTime: when event was ingested into KQL DB

Analysis focus:
- what operation occurred: type
- which item was affected: itemName, itemKind
- which workspace was involved: workspaceName
- who executed the action: executingPrincipalType
- when event occurred: time
- when available for analytics: IngestionTime

Use filters and summarize only when user asks.
Keep query safe and read-only.
""";

        var payload = new
        {
            model,
            temperature = 0.1,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = prompt }
            }
        };

        var endpoints = BuildCandidateEndpoints(baseUrl);
        string? content = null;
        string? query = null;
        Exception? lastError = null;
        foreach (var endpoint in endpoints)
        {
            try
            {
                content = await TrySendAsync(endpoint, payload, apiKey, model, systemPrompt, prompt);
                query = ExtractQueryFromResponse(content)?.Trim();
                if (!string.IsNullOrWhiteSpace(query))
                {
                    break;
                }

                lastError = new InvalidOperationException($"Endpoint returned empty query: {endpoint}");
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new InvalidOperationException(
                $"LM Studio request failed for all endpoints: {string.Join(", ", endpoints)} | Last error: {lastError?.Message}");
        }
        return query;
    }

    private async Task<string> TrySendAsync(string endpoint, object payload, string apiKey, string model, string systemPrompt, string userPrompt)
    {
        try
        {
            using var client = CreateLocalClient(apiKey);
            return await SendAsync(client, endpoint, payload, model, systemPrompt, userPrompt);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase))
        {
            if (endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var httpEndpoint = "http://" + endpoint["https://".Length..];
                using var fallbackClient = CreateLocalClient(apiKey);
                return await SendAsync(fallbackClient, httpEndpoint, payload, model, systemPrompt, userPrompt);
            }

            throw;
        }
    }

    private static HttpClient CreateLocalClient(string apiKey)
    {
        var handler = new HttpClientHandler
        {
            UseProxy = false,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(45) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return client;
    }

    private static List<string> BuildCandidateEndpoints(string baseUrl)
    {
        var clean = baseUrl.Trim().TrimEnd('/');
        var candidates = new List<string>();

        void AddIfMissing(string endpoint)
        {
            if (!candidates.Contains(endpoint, StringComparer.OrdinalIgnoreCase))
            {
                candidates.Add(endpoint);
            }
        }

        AddIfMissing($"{clean}/api/v1/chat");
        AddIfMissing($"{clean}/chat/completions");

        if (clean.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
            clean.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            clean.Contains("host.docker.internal", StringComparison.OrdinalIgnoreCase))
        {
            var httpClean = clean
                .Replace("https://localhost", "http://localhost", StringComparison.OrdinalIgnoreCase)
                .Replace("https://127.0.0.1", "http://127.0.0.1", StringComparison.OrdinalIgnoreCase)
                .Replace("https://host.docker.internal", "http://host.docker.internal", StringComparison.OrdinalIgnoreCase);
            AddIfMissing($"{httpClean}/api/v1/chat");
            AddIfMissing($"{httpClean}/chat/completions");

            if (!httpClean.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                AddIfMissing($"{httpClean}/v1/chat/completions");
                AddIfMissing($"{httpClean}/api/v1/chat");
            }

            AddIfMissing("http://127.0.0.1:1234/v1/chat/completions");
            AddIfMissing("http://localhost:1234/v1/chat/completions");
            AddIfMissing("http://127.0.0.1:1234/api/v1/chat");
            AddIfMissing("http://localhost:1234/api/v1/chat");
            AddIfMissing("http://host.docker.internal:1234/v1/chat/completions");
            AddIfMissing("http://host.docker.internal:1234/api/v1/chat");
        }

        return candidates;
    }

    private static async Task<string> SendAsync(HttpClient client, string endpoint, object payload, string model, string systemPrompt, string userPrompt)
    {
        var requestBody = payload;
        if (endpoint.EndsWith("/api/v1/chat", StringComparison.OrdinalIgnoreCase))
        {
            requestBody = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.1
            };
        }

        var response = await client.PostAsync(
            endpoint,
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"LM Studio request failed: {(int)response.StatusCode} {response.ReasonPhrase} | {content}");
        }

        return content;
    }

    private static string? ExtractQueryFromResponse(string content)
    {
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
        {
            var first = choices[0];
            if (first.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var msgContent))
            {
                return msgContent.GetString();
            }

            if (first.TryGetProperty("text", out var textContent))
            {
                return textContent.GetString();
            }
        }

        if (root.TryGetProperty("message", out var topMessage) &&
            topMessage.TryGetProperty("content", out var topContent))
        {
            return topContent.GetString();
        }

        if (root.TryGetProperty("text", out var topText))
        {
            return topText.GetString();
        }

        return null;
    }
}
