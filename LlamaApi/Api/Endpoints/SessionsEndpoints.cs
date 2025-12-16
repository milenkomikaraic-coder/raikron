using LlamaApi.Services.Sessions;
using LlamaApi.Api.DTOs.Responses;

namespace LlamaApi.Api.Endpoints;

public static class SessionsEndpoints
{
    public static void MapSessionsEndpoints(this WebApplication app)
    {
        app.MapPost("/sessions/{id}/reset", async (SessionService sessions, string id) =>
        {
            await sessions.ResetSessionAsync(id);
            return Results.Ok(new SessionResetResponse(id, Reset: true));
        })
        .WithSummary("Reset a chat session")
        .WithDescription("Resets the conversation context for a session ID, clearing the chat history. Use this to start a fresh conversation with the same session ID. Path parameter 'id' is the session ID (e.g., 'session-123').")
        .Produces<object>(StatusCodes.Status200OK)
        .WithTags("Sessions");
    }
}
