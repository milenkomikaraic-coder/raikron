using System.ComponentModel.DataAnnotations;

namespace LlamaApi.Api.DTOs.Requests;

public record DownloadRequest(
    [Required(ErrorMessage = "ModelId is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "ModelId must be between 1 and 200 characters")]
    string ModelId,
    
    [Required(ErrorMessage = "Source is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Source must be between 1 and 500 characters")]
    string Source,
    
    [StringLength(100, ErrorMessage = "Checksum must not exceed 100 characters")]
    string? Checksum = null,
    
    [StringLength(50, ErrorMessage = "Priority must not exceed 50 characters")]
    string? Priority = null);
