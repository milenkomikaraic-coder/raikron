namespace LlamaApi.Api.DTOs.Responses;

public record DownloadResponse(DownloadJobResponse? Job, ModelInfoResponse? Model);

public record DownloadJobResponse(string JobId);

public record ModelInfoResponse(
    string ModelId,
    string Status,
    long SizeBytes,
    bool OnDisk,
    bool Active
);
