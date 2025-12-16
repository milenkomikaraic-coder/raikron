using LlamaApi.Services.Jobs;
using LlamaApi.Api.DTOs.Responses;

namespace LlamaApi.Api.Endpoints;

public static class JobsEndpoints
{
    public static void MapJobsEndpoints(this WebApplication app)
    {
        app.MapGet("/jobs/{id}", async (JobService jobs, string id) =>
        {
            var job = await jobs.GetJobAsync(id);
            if (job == null)
                return Results.NotFound(new ErrorResponse(new ErrorDetail("not_found", "Job not found")));
            return Results.Ok(job);
        })
        .WithSummary("Get download job status")
        .WithDescription("Retrieves the status of an asynchronous download job. Returns job status (queued, running, succeeded, failed), progress (0.0-1.0), and error message if failed. Use this endpoint to poll download progress after receiving a 202 response from POST /models/download. Example response: {\"jobId\": \"abc-123\", \"status\": \"running\", \"progress\": 0.45, \"error\": null}. Path parameter 'id' is the job ID from the download response.")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status404NotFound)
        .WithTags("Jobs");
    }
}
