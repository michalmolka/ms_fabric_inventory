using Microsoft.ML.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace blz0.Models
{
    [NotMapped]
    public class EventLogsCount
    {
        public DateOnly? EventTime { get; set; } // Nullable DateOnly property
        public int Cnt { get; set; } // Count property
    }

    [NotMapped ]
    public class AnomalyPrediction
    {
        [VectorType(3)]
        public double[]? Prediction { get; set; }
    }

    [NotMapped]
    public class EventLogsCountAnomaly
    {
        [LoadColumn(0)]
        public DateTime TimeStamp { get; set; } // Nullable DateOnly property
        [LoadColumn(1)]
        public float Value { get; set; } // Count property
    }


    [NotMapped]
    public class EventLogs
    {
        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        [JsonPropertyName("EventTime")]
        public DateTime? EventTime { get; set; }

        [JsonPropertyName("EventType")]
        public string? EventType { get; set; }

        [JsonPropertyName("ItemId")]
        public string? ItemId { get; set; }


        [JsonPropertyName("ItemName")]
        public string? ItemName { get; set; }

        [JsonPropertyName("WorkspaceId")]
        public string? WorkspaceId { get; set; }

        [JsonPropertyName("WorkspaceName")]
        public string? WorkspaceName { get; set; }

        [JsonPropertyName("ItemKind")]
        public string? ItemKind { get; set; }
    }
}
