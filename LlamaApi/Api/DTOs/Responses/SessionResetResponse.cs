namespace LlamaApi.Api.DTOs.Responses;

public record SessionResetResponse(
    string SessionId,
    bool Reset
);
