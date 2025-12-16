namespace LlamaApi.Api.DTOs.Responses;

public record HealthResponse(
    string Status,
    GpuInfo Gpu,
    CpuInfo Cpu,
    string? ActiveModel
);

public record GpuInfo(string Name, ulong VramBytes, bool CudaCapable);

public record CpuInfo(string Name, int Cores);
