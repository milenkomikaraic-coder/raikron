namespace LlamaApi.Core.Domain;

public enum JobStatusEnum
{
    Queued,
    Running,
    Succeeded,
    Failed
}

public static class JobStatusEnumExtensions
{
    public static string ToStringValue(this JobStatusEnum status) => status switch
    {
        JobStatusEnum.Queued => "queued",
        JobStatusEnum.Running => "running",
        JobStatusEnum.Succeeded => "succeeded",
        JobStatusEnum.Failed => "failed",
        _ => "queued"
    };

    public static JobStatusEnum FromString(string? status) => status?.ToLowerInvariant() switch
    {
        "queued" => JobStatusEnum.Queued,
        "running" => JobStatusEnum.Running,
        "succeeded" => JobStatusEnum.Succeeded,
        "failed" => JobStatusEnum.Failed,
        _ => JobStatusEnum.Queued
    };
}
