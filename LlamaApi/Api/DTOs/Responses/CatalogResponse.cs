using LlamaApi.Services.LLMs;

namespace LlamaApi.Api.DTOs.Responses;

public record CatalogResponse(
    List<CatalogModel> Models,
    int Count,
    IEnumerable<CategoryInfo> Categories,
    DateTime? LastUpdated
);

public record CategoryInfo(string Category, int Count);
