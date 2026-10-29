using blz0.Models;
using System.Text.Json;

namespace blz0.Operations
{
    public class FabricApiData
    {
        public IEnumerable<Workspace>? workspacesList;
        public IEnumerable<Dataset>? datasetsList;
        public IEnumerable<Capacity>? capacitiesList;
        public IEnumerable<Report>? reportsList;
        public IEnumerable<Lakehouse>? lakehousesList;
        public IEnumerable<Warehouse>? warehousesList;


        public async Task RefreshDatasetAsync(HttpClient clientToken, string datasetId, string? workspaceId)
        {
            Console.WriteLine("Refreshing dataset...");
            var url = string.IsNullOrWhiteSpace(workspaceId)
                ? $"https://api.powerbi.com/v1.0/myorg/datasets/{datasetId}/refreshes"
                : $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/refreshes";
            var response = await clientToken.PostAsync(url, null);
            Console.WriteLine($"Response: { response.Headers.Location?.ToString()}");
            Console.WriteLine($"Response: {response}");

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Fabric API request failed: {(int)response.StatusCode} {response.ReasonPhrase} | {url} | {responseBody}");
            }
        }
        public async Task GetCapacitiesApiAsync(HttpClient clientToken)
        {
            string jsonString;
            jsonString = await GetStringOrThrowAsync(clientToken, "https://api.powerbi.com/v1.0/myorg/capacities");
            //jsonString = reportsResponse.Result;

            Deserialization capacitiesObject = new Deserialization();
            this.capacitiesList = capacitiesObject.DeserializeCapacity(jsonString);
        }

        public async Task GetReportsApiAsync(HttpClient clientToken, string? workspaceIdNav)
        {
            string jsonString;
            
            if (workspaceIdNav == null)
            {
                jsonString = await GetStringOrThrowAsync(clientToken, "https://api.powerbi.com/v1.0/myorg/reports");
            }
            else
            {
                jsonString = await GetStringOrThrowAsync(clientToken, $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceIdNav}/reports");
            }
            Deserialization reportsObject = new Deserialization();
            this.reportsList = reportsObject.DeserializeReport(jsonString);
        }

        public async Task GetWorkspacesApiAsync(HttpClient clientToken, string? capacityIdNav)
        {
            string jsonString;
            //Task<string> reportsResponse;

            if (capacityIdNav == null)
            {
                jsonString = await GetStringOrThrowAsync(clientToken, "https://api.powerbi.com/v1.0/myorg/groups");
            }
            else
            {
                jsonString = await GetStringOrThrowAsync(clientToken, $"https://api.powerbi.com/v1.0/myorg/groups?$filter=contains(capacityId,'{capacityIdNav}')");
            }

            //jsonString = reportsResponse.Result;

            Deserialization workspacesObject = new Deserialization();
            this.workspacesList = workspacesObject.DeserializeWorkspace(jsonString);
        }

        public async Task GetDatasetsApiAsync(HttpClient clientToken, string? workspaceIdNav)
        {
            string jsonString;
            //Task<string> reportsResponse;

            if (workspaceIdNav == null)
            {
                //reportsResponse = clientToken!.GetStringAsync($"https://api.powerbi.com/v1.0/myorg/admin/datasets");
                jsonString = await GetStringOrThrowAsync(clientToken, "https://api.powerbi.com/v1.0/myorg/datasets");
            }
            else
            {
                jsonString = await GetStringOrThrowAsync(clientToken, $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceIdNav}/datasets");
            } 
        
            //jsonString = reportsResponse.Result;
            Deserialization datasetsObject = new Deserialization();
            this.datasetsList = datasetsObject.DeserializeDataset(jsonString);
        }

        public async Task GetLakehousesApiAsync(HttpClient clientToken, string workspaceId)
        {
            var items = await GetPagedItemsOrThrowAsync<Lakehouse>(
                clientToken,
                $"https://api.fabric.microsoft.com/v1/workspaces/{workspaceId}/lakehouses");

            foreach (var lakehouse in items)
            {
                lakehouse.DefaultSchema = lakehouse.Properties?.DefaultSchema;
                lakehouse.OneLakeTablesPath = lakehouse.Properties?.OneLakeTablesPath;
                lakehouse.OneLakeFilesPath = lakehouse.Properties?.OneLakeFilesPath;
                lakehouse.SqlEndpointId = lakehouse.Properties?.SqlEndpointProperties?.Id;
                lakehouse.SqlEndpointConnectionString = lakehouse.Properties?.SqlEndpointProperties?.ConnectionString;
                lakehouse.SqlEndpointProvisioningStatus = lakehouse.Properties?.SqlEndpointProperties?.ProvisioningStatus;
            }

            lakehousesList = items;
        }

        public async Task<List<LakehouseTable>> GetLakehouseTablesApiAsync(HttpClient clientToken, string workspaceId, string lakehouseId)
        {
            return await GetPagedItemsOrThrowAsync<LakehouseTable>(
                clientToken,
                $"https://api.fabric.microsoft.com/v1/workspaces/{workspaceId}/lakehouses/{lakehouseId}/tables?maxResults=100");
        }

        public async Task GetWarehousesApiAsync(HttpClient clientToken, string workspaceId)
        {
            var items = await GetPagedItemsOrThrowAsync<Warehouse>(
                clientToken,
                $"https://api.fabric.microsoft.com/v1/workspaces/{workspaceId}/warehouses");

            foreach (var warehouse in items)
            {
                warehouse.ConnectionString = warehouse.Properties?.ConnectionString;
                warehouse.CreatedDate = warehouse.Properties?.CreatedDate;
                warehouse.LastUpdatedTime = warehouse.Properties?.LastUpdatedTime;
                warehouse.CollationType = warehouse.Properties?.CollationType;
            }

            warehousesList = items;
        }

        public async Task<DatasetRefreshItem?> GetDatasetLastRefreshAsync(HttpClient clientToken, string datasetId, string? workspaceId)
        {
            var url = string.IsNullOrWhiteSpace(workspaceId)
                ? $"https://api.powerbi.com/v1.0/myorg/datasets/{datasetId}/refreshes?$top=1"
                : $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/refreshes?$top=1";
            var json = await GetStringOrThrowAsync(clientToken, url);

            var payload = System.Text.Json.JsonSerializer.Deserialize<DatasetRefreshRegister>(json);
            return payload?.Value?.FirstOrDefault();
        }

        public async Task<List<DatasetRefreshItem>> GetDatasetRefreshHistoryAsync(HttpClient clientToken, string datasetId, string workspaceId, int top = 20)
        {
            var json = await GetStringOrThrowAsync(clientToken, $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/refreshes?$top={top}");
            var payload = System.Text.Json.JsonSerializer.Deserialize<DatasetRefreshRegister>(json);
            return payload?.Value ?? [];
        }

        public async Task<List<Report>> GetReportsUsingDatasetAsync(HttpClient clientToken, string workspaceId, string datasetId)
        {
            var json = await GetStringOrThrowAsync(clientToken, $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/reports");
            var payload = System.Text.Json.JsonSerializer.Deserialize<ReportsRegister>(json);
            var reports = payload?.Report ?? [];

            foreach (var report in reports)
            {
                report.WorkspaceId = workspaceId;
            }

            return reports
                .Where(e => string.Equals(e.DatasetId, datasetId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task<Capacity?> GetCapacityByIdAsync(HttpClient clientToken, string capacityId)
        {
            await GetCapacitiesApiAsync(clientToken);
            return capacitiesList?.FirstOrDefault(e => string.Equals(e.Id, capacityId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<Workspace?> GetWorkspaceByIdAsync(HttpClient clientToken, string workspaceId)
        {
            var jsonString = await GetStringOrThrowAsync(clientToken, $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}");
            return System.Text.Json.JsonSerializer.Deserialize<Workspace>(jsonString);
        }

        public async Task<Dataset?> GetDatasetByIdAsync(HttpClient clientToken, string workspaceId, string datasetId)
        {
            var jsonString = await GetStringOrThrowAsync(clientToken, $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}");
            return System.Text.Json.JsonSerializer.Deserialize<Dataset>(jsonString);
        }

        public async Task<Report?> GetReportByIdAsync(HttpClient clientToken, string workspaceId, string reportId)
        {
            var jsonString = await GetStringOrThrowAsync(clientToken, $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/reports/{reportId}");
            return System.Text.Json.JsonSerializer.Deserialize<Report>(jsonString);
        }

        private static async Task<string> GetStringOrThrowAsync(HttpClient clientToken, string url)
        {
            var response = await clientToken.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Fabric API request failed: {(int)response.StatusCode} {response.ReasonPhrase} | {url} | {responseBody}");
        }

        private static async Task<List<T>> GetPagedItemsOrThrowAsync<T>(HttpClient clientToken, string url)
        {
            var items = new List<T>();
            var nextUrl = url;

            while (!string.IsNullOrWhiteSpace(nextUrl))
            {
                var json = await GetStringOrThrowAsync(clientToken, nextUrl);
                var page = JsonSerializer.Deserialize<FabricItemsPage<T>>(json);
                var pageItems = page?.Items;
                if (pageItems != null)
                {
                    items.AddRange(pageItems);
                }

                nextUrl = page?.ContinuationUri;
            }

            return items;
        }

        private sealed class FabricItemsPage<T>
        {
            [System.Text.Json.Serialization.JsonPropertyName("value")]
            public List<T>? Value { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("data")]
            public List<T>? Data { get; set; }

            [System.Text.Json.Serialization.JsonIgnore]
            public List<T>? Items => Value ?? Data;

            [System.Text.Json.Serialization.JsonPropertyName("continuationUri")]
            public string? ContinuationUri { get; set; }
        }
    }
}
