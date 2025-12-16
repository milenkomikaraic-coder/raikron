namespace LlamaApi.Infrastructure.Workers.Download;

public record DownloadJob(
    string JobId,
    string DownloadUrl,
    string FilePath,
    string ModelId,
    string Source,
    long ExpectedSize,
    string? Checksum
);
