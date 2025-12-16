using System.Text.Json.Serialization;

namespace LlamaApi.Core.Domain;

public class Job
{
    public string JobId { get; set; } = "";
    
    [JsonConverter(typeof(JobStatusEnumJsonConverter))]
    public JobStatusEnum Status { get; set; } = JobStatusEnum.Queued;
    
    public double Progress { get; set; }
    public string? Error { get; set; }
}
