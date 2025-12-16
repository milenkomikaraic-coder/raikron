namespace LlamaApi.Core.Constants;

public static class Thresholds
{
    /// <summary>
    /// File size threshold (2 GB) below which downloads are synchronous
    /// </summary>
    public const long SyncDownloadThresholdBytes = 2L * 1024 * 1024 * 1024;

    /// <summary>
    /// Cache expiry time for model catalog (1 hour)
    /// </summary>
    public static readonly TimeSpan CatalogCacheExpiry = TimeSpan.FromHours(1);
}
