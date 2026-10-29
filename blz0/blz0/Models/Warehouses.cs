using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace blz0.Models
{
    public class WarehousesRegister
    {
        [JsonPropertyName("value")]
        public List<Warehouse>? Warehouses { get; set; }
    }

    public class Warehouse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("workspaceId")]
        public string? WorkspaceId { get; set; }

        [JsonPropertyName("folderId")]
        public string? FolderId { get; set; }

        [JsonPropertyName("properties")]
        [NotMapped]
        public WarehouseProperties? Properties { get; set; }

        public string? ConnectionString { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastUpdatedTime { get; set; }
        public string? CollationType { get; set; }
        public DateTime? InsertDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string UserObjectId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    [NotMapped]
    public class WarehouseTable
    {
        public string? SchemaName { get; set; }
        public string? TableName { get; set; }
        public string? TableType { get; set; }
    }

    [NotMapped]
    public class WarehouseProperties
    {
        [JsonPropertyName("connectionString")]
        public string? ConnectionString { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime? CreatedDate { get; set; }

        [JsonPropertyName("lastUpdatedTime")]
        public DateTime? LastUpdatedTime { get; set; }

        [JsonPropertyName("collationType")]
        public string? CollationType { get; set; }
    }
}
