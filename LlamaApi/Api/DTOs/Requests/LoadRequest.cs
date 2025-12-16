using System.ComponentModel.DataAnnotations;

namespace LlamaApi.Api.DTOs.Requests;

public record LoadRequest(
    [Required(ErrorMessage = "ModelId is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "ModelId must be between 1 and 200 characters")]
    string ModelId,
    
    [Range(1, 100000, ErrorMessage = "NCtx must be between 1 and 100000")]
    int? NCtx = null,
    
    [Range(1, 10000, ErrorMessage = "Batch must be between 1 and 10000")]
    int? Batch = null,
    
    [Range(0, 1000, ErrorMessage = "NGpuLayers must be between 0 and 1000")]
    int? NGpuLayers = null,
    
    [StringLength(20, ErrorMessage = "Offload must not exceed 20 characters")]
    string? Offload = null);
