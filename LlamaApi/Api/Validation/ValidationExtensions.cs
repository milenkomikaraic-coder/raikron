using System.ComponentModel.DataAnnotations;
using LlamaApi.Api.DTOs.Requests;
using LlamaApi.Api.DTOs.Responses;

namespace LlamaApi.Api.Validation;

public static class ValidationExtensions
{
    public static IResult? ValidateModel<T>(T model) where T : class
    {
        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        
        if (!Validator.TryValidateObject(model, validationContext, validationResults, true))
        {
            var errorMessage = string.Join("; ", validationResults.Select(r => r.ErrorMessage ?? "Validation failed"));
            return Results.BadRequest(new ErrorResponse(new ErrorDetail("validation", errorMessage)));
        }
        
        // Validate nested objects for ChatRequest
        if (model is ChatRequest chatRequest && chatRequest.Messages != null)
        {
            foreach (var message in chatRequest.Messages)
            {
                var messageContext = new ValidationContext(message);
                var messageResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(message, messageContext, messageResults, true))
                {
                    var messageErrors = string.Join("; ", messageResults.Select(r => r.ErrorMessage ?? "Validation failed"));
                    return Results.BadRequest(new ErrorResponse(new ErrorDetail("validation", $"Message validation failed: {messageErrors}")));
                }
            }
        }
        
        return null;
    }
}
