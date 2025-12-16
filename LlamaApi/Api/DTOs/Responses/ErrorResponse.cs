namespace LlamaApi.Api.DTOs.Responses;

public record ErrorResponse(ErrorDetail Error);

public record ErrorDetail(string Code, string Message);
