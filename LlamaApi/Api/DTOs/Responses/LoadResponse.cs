namespace LlamaApi.Api.DTOs.Responses;

public record LoadResponse(
    string ModelId,
    bool Loaded,
    object? Params
);
