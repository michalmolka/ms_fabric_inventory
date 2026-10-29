using System.Text.Json.Serialization;

namespace blz0.Models
{
    public class ReportsRegister
    {
        [JsonPropertyName("@odata.context")]
        public string? OdataContext { get; set; }

        [JsonPropertyName("value")]
        public List<Report>? Report { get; set; }
    }
    public class Report
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("reportType")]
        public string? ReportType { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("isFromPbix")]
        public bool? IsFromPbix { get; set; }

        [JsonPropertyName("datasetId")]
        public string? DatasetId { get; set; }

        [JsonPropertyName("datasetWorkspaceId")]
        public string? DatasetWorkspaceId { get; set; }

        public DateTime? InsertDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string UserObjectId { get; set; } = string.Empty;
        public string? WorkspaceId { get; set; }
        public bool IsActive { get; set; } = true;

    }
}
