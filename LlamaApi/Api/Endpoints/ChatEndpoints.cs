using LlamaApi.Api.DTOs.Requests;
using LlamaApi.Api.DTOs.Responses;
using LlamaApi.Api.Validation;
using LlamaApi.Services.LLMs;
using LlamaApi.Services.Sessions;

namespace LlamaApi.Api.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        app.MapPost("/chat", async (ModelManagerService modelMgr, SessionService sessions, ChatRequest body, HttpRequest request, HttpResponse response) =>
        {
            if (body == null)
                return Results.BadRequest(new ErrorResponse(new ErrorDetail("validation", "Invalid request body")));
            
            var validationResult = ValidationExtensions.ValidateModel(body);
            if (validationResult != null)
                return validationResult;

            var isSse = request.Headers.Accept.ToString().Contains("text/event-stream");
            return await HandleChat(modelMgr, sessions, body, response, isSse);
        })
        .WithSummary("Chat completion endpoint")
        .WithDescription("Generates a chat completion using the active model (or specified modelId). Supports both SSE (Server-Sent Events) and NDJSON streaming. Set Accept: text/event-stream header for SSE, or omit for NDJSON. You can use either 'prompt' for simple text or 'messages' array for conversation history. Example request body (copy-paste ready): {\"modelId\": \"tinyllama\", \"prompt\": \"Hello, how are you?\", \"maxTokens\": 100, \"temperature\": 0.7, \"topP\": 0.95}")
        .Accepts<ChatRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .WithTags("Chat");

        app.MapPost("/chat/stream", async (ModelManagerService modelMgr, SessionService sessions, ChatRequest body, HttpRequest request, HttpResponse response) =>
        {
            if (body == null)
                return Results.BadRequest(new ErrorResponse(new ErrorDetail("validation", "Invalid request body")));
            
            var validationResult = ValidationExtensions.ValidateModel(body);
            if (validationResult != null)
                return validationResult;

            return await HandleChat(modelMgr, sessions, body, response, isSse: true);
        })
        .WithSummary("Chat completion with SSE streaming")
        .WithDescription("Same as /chat but always returns SSE (Server-Sent Events) format. Use this endpoint when you want guaranteed SSE streaming. Example request body (copy-paste ready): {\"modelId\": \"tinyllama\", \"messages\": [{\"role\": \"user\", \"content\": \"What is AI?\"}], \"maxTokens\": 200, \"temperature\": 0.8}")
        .Accepts<ChatRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .WithTags("Chat");
    }

    private static async Task<IResult> HandleChat(ModelManagerService modelMgr, SessionService sessions, ChatRequest body, HttpResponse response, bool isSse)
    {
        var modelId = body.ModelId ?? modelMgr.GetActiveModelId();
        if (string.IsNullOrEmpty(modelId))
            return Results.NotFound(new ErrorResponse(new ErrorDetail("not_found", "No active model")));

        var result = await modelMgr.ChatAsync(modelId, body, sessions);
        if (!result.Success)
            return Results.UnprocessableEntity(new ErrorResponse(new ErrorDetail(result.ErrorCode ?? "unknown", result.ErrorMessage ?? "Unknown error")));

        if (isSse)
        {
            response.ContentType = "text/event-stream";
            var escapedTokens = System.Text.Json.JsonSerializer.Serialize(result.Tokens);
            await response.WriteAsync($"event: token\ndata: {escapedTokens}\n\n");
            var endData = System.Text.Json.JsonSerializer.Serialize(new { usage = result.Usage, timings = result.Timings });
            await response.WriteAsync($"event: end\ndata: {endData}\n\n");
            await response.Body.FlushAsync();
            return Results.Empty;
        }
        else
        {
            // NDJSON
            response.ContentType = "application/x-ndjson";
            var tokenJson = System.Text.Json.JsonSerializer.Serialize(new { type = "token", text = result.Tokens });
            await response.WriteAsync($"{tokenJson}\n");
            var endJson = System.Text.Json.JsonSerializer.Serialize(new { type = "end", usage = result.Usage, timings = result.Timings });
            await response.WriteAsync($"{endJson}\n");
            await response.Body.FlushAsync();
            return Results.Empty;
        }
    }
}
