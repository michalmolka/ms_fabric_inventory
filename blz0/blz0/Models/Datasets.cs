using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace blz0.Models
{
    public class DatasetsRegister
    {
        [JsonPropertyName("@odata.context")]
        public string? OdataContext { get; set; }

        [JsonPropertyName("value")]
        public List<Dataset>? Dataset { get; set; }
    }
    public class Dataset
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("webUrl")]
        public string? WebUrl { get; set; }

        [JsonPropertyName("addRowsAPIEnabled")]
        public bool AddRowsAPIEnabled { get; set; }

        [JsonPropertyName("configuredBy")]
        public string? ConfiguredBy { get; set; }

        [JsonPropertyName("isRefreshable")]
        public bool IsRefreshable { get; set; }

        [JsonPropertyName("isEffectiveIdentityRequired")]
        public bool IsEffectiveIdentityRequired { get; set; }

        [JsonPropertyName("isEffectiveIdentityRolesRequired")]
        public bool IsEffectiveIdentityRolesRequired { get; set; }

        [JsonPropertyName("isOnPremGatewayRequired")]
        public bool IsOnPremGatewayRequired { get; set; }

        [JsonPropertyName("targetStorageMode")]
        public string? TargetStorageMode { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("createReportEmbedURL")]
        public string? CreateReportEmbedURL { get; set; }

        [JsonPropertyName("qnaEmbedURL")]
        public string? QnaEmbedURL { get; set; }

        [JsonPropertyName("upstreamDatasets")]
        public List<int>? UpstreamDatasets { get; set; }

        [JsonPropertyName("users")]
        public List<string>? Users { get; set; }

        [JsonPropertyName("queryScaleOutSettings")]
        [NotMapped]
        public QueryScaleOutSettings? QueryScaleOutSettings { get; set; }

        public DateTime? InsertDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string UserObjectId { get; set; } = string.Empty;
        public string? WorkspaceId { get; set; }
        public DateTime? LastRefreshAtUtc { get; set; }
        public string? LastRefreshStatus { get; set; }
        public DateTime? LastRefreshCacheUpdatedAtUtc { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [NotMapped]
    public class QueryScaleOutSettings
    {
        [JsonPropertyName("autoSyncReadOnlyReplicas")]
        public bool AutoSyncReadOnlyReplicas { get; set; }
        [JsonPropertyName("maxReadOnlyReplicas")]
        public int MaxReadOnlyReplicas { get; set; }
    }

    public class DatasetRefreshRegister
    {
        [JsonPropertyName("value")]
        public List<DatasetRefreshItem>? Value { get; set; }
    }

    public class DatasetRefreshItem
    {
        [JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTime? EndTime { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("refreshType")]
        public string? RefreshType { get; set; }
    }
}
