namespace LlamaApi.Core.Domain;

public enum ModelStatus
{
    Available,
    Downloading,
    Loaded,
    Error
}

public static class ModelStatusExtensions
{
    public static string ToStringValue(this ModelStatus status) => status switch
    {
        ModelStatus.Available => "available",
        ModelStatus.Downloading => "downloading",
        ModelStatus.Loaded => "loaded",
        ModelStatus.Error => "error",
        _ => "available"
    };

    public static ModelStatus FromString(string? status) => status?.ToLowerInvariant() switch
    {
        "available" => ModelStatus.Available,
        "downloading" => ModelStatus.Downloading,
        "loaded" => ModelStatus.Loaded,
        "error" => ModelStatus.Error,
        _ => ModelStatus.Available
    };
}
