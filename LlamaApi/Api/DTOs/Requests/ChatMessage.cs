using System.ComponentModel.DataAnnotations;

namespace LlamaApi.Api.DTOs.Requests;

public record ChatMessage(
    [Required(ErrorMessage = "Role is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Role must be between 1 and 50 characters")]
    string Role,
    
    [Required(ErrorMessage = "Content is required")]
    [StringLength(50000, MinimumLength = 1, ErrorMessage = "Content must be between 1 and 50000 characters")]
    string Content);
