using System.Text.Json.Serialization;

namespace LlamaApi.Core.Domain;

public class ModelEntry
{
    public string ModelId { get; set; } = "";
    public string? Source { get; set; }
    public long SizeBytes { get; set; }
    
    [JsonConverter(typeof(ModelStatusJsonConverter))]
    public ModelStatus Status { get; set; } = ModelStatus.Available;
    
    public bool OnDisk { get; set; }
    public bool Active { get; set; }
}
