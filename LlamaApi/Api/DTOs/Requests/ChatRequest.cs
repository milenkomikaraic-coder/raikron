using System.ComponentModel.DataAnnotations;

namespace LlamaApi.Api.DTOs.Requests;

public record ChatRequest(
    [StringLength(200, ErrorMessage = "ModelId must not exceed 200 characters")]
    string? ModelId = null,
    
    [StringLength(200, ErrorMessage = "SessionId must not exceed 200 characters")]
    string? SessionId = null,
    
    ChatMessage[]? Messages = null,
    
    [StringLength(10000, ErrorMessage = "Prompt must not exceed 10000 characters")]
    string? Prompt = null,
    
    [Range(1, 100000, ErrorMessage = "MaxTokens must be between 1 and 100000")]
    int MaxTokens = 1024,
    
    [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0")]
    double Temperature = 0.7,
    
    [Range(0.0, 1.0, ErrorMessage = "TopP must be between 0.0 and 1.0")]
    double TopP = 0.95,
    
    string[]? Stop = null,
    
    [Range(1, 100000, ErrorMessage = "NCtx must be between 1 and 100000")]
    int? NCtx = null);
