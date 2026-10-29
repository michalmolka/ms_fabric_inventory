using System.Text.Json.Serialization;

namespace blz0.Models
{
    public class CapacitiesRegister
    {
        [JsonPropertyName("@odata.context")]
        public string? OdataContext { get; set; }
        [JsonPropertyName("value")]
        public List<Capacity>? Capacity { get; set; }
    }
    public class Capacity
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("sku")]
        public string? Sku { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("capacityUserAccessRight")]
        public string? CapacityUserAccessRight { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        public DateTime? InsertDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string UserObjectId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        //public ICollection<Workspace> Workspaces { get; } = new List<Workspace>();
    }
}
