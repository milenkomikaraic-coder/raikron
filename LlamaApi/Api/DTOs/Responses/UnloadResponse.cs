namespace LlamaApi.Api.DTOs.Responses;

public record UnloadResponse(
    string ModelId,
    bool Unloaded
);
