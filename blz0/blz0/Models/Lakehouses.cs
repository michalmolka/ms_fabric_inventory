using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace blz0.Models
{
    public class LakehousesRegister
    {
        [JsonPropertyName("value")]
        public List<Lakehouse>? Lakehouses { get; set; }
    }

    public class Lakehouse
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
        public LakehouseProperties? Properties { get; set; }

        public string? DefaultSchema { get; set; }
        public string? OneLakeTablesPath { get; set; }
        public string? OneLakeFilesPath { get; set; }
        public string? SqlEndpointId { get; set; }
        public string? SqlEndpointConnectionString { get; set; }
        public string? SqlEndpointProvisioningStatus { get; set; }
        public DateTime? InsertDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string UserObjectId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    [NotMapped]
    public class LakehouseTable
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }
    }

    [NotMapped]
    public class LakehouseProperties
    {
        [JsonPropertyName("defaultSchema")]
        public string? DefaultSchema { get; set; }

        [JsonPropertyName("oneLakeTablesPath")]
        public string? OneLakeTablesPath { get; set; }

        [JsonPropertyName("oneLakeFilesPath")]
        public string? OneLakeFilesPath { get; set; }

        [JsonPropertyName("sqlEndpointProperties")]
        public LakehouseSqlEndpointProperties? SqlEndpointProperties { get; set; }
    }

    [NotMapped]
    public class LakehouseSqlEndpointProperties
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("connectionString")]
        public string? ConnectionString { get; set; }

        [JsonPropertyName("provisioningStatus")]
        public string? ProvisioningStatus { get; set; }
    }
}
