using System.ComponentModel.DataAnnotations;

namespace LlamaApi.Api.DTOs.Requests;

public record ActiveRequest(
    [Required(ErrorMessage = "ModelId is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "ModelId must be between 1 and 200 characters")]
    string ModelId);
