using System.Text.Json.Serialization;

namespace blz0.Models
{
    public class WorkspacesRegister
    {
        [JsonPropertyName("@odata.context")]
        public string? OdataContext { get; set; }

        [JsonPropertyName("value")]
        public List<Workspace>? Workspace { get; set; }
    }
    public class Workspace
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("isReadOnly")]
        public bool? IsReadOnly { get; set; }

        [JsonPropertyName("isOnDedicatedCapacity")]
        public bool? IsOnDedicatedCapacity { get; set; }

        [JsonPropertyName("capacityId")]
        public string? CapacityId { get; set; }

        [JsonPropertyName("defaultDatasetStorageFormat")]
        public string? DefaultDatasetStorageFormat { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
        public DateTime? InsertDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string UserObjectId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        //public ICollection<Dataset> Datasets { get; } = new List<Dataset>();
        //public Capacity Capacity { get; set; }
    }
}
