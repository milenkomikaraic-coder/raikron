using LlamaApi.Core.Domain;

namespace LlamaApi.Api.DTOs.Responses;

public record ModelsResponse(List<ModelEntry> Models);
