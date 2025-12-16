using LlamaApi.Services.Hardware;
using LlamaApi.Services.LLMs;
using LlamaApi.Services.Observability;
using LlamaApi.Api.DTOs.Responses;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LlamaApi.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", async (HardwareDetectionService hardware, ModelManagerService modelMgr, HealthCheckService healthChecks) =>
        {
            var hw = hardware.GetHardwareInfo();
            var activeModel = modelMgr.GetActiveModelId();
            var healthReport = await healthChecks.CheckHealthAsync();
            
            return Results.Ok(new HealthResponse(
                Status: healthReport.Status == HealthStatus.Healthy ? "ok" : healthReport.Status == HealthStatus.Degraded ? "degraded" : "unhealthy",
                Gpu: new GpuInfo(hw.GpuName, hw.VramBytes, hw.CudaCapable),
                Cpu: new CpuInfo(hw.CpuName, hw.CpuCores),
                ActiveModel: activeModel
            ));
        })
        .WithSummary("Health check with hardware information")
        .WithDescription("Returns API health status along with detected GPU/CPU information and the currently active model ID. Use this endpoint to verify the API is running and check available hardware resources.")
        .WithTags("Health")
        .Produces<object>(StatusCodes.Status200OK);

        // ASP.NET Core health checks endpoint
        app.MapHealthChecks("/health/ready");
        app.MapHealthChecks("/health/live");

        app.MapGet("/metrics", (MetricsService metrics) =>
        {
            return Results.Text(metrics.GetPrometheusText(), "text/plain");
        })
        .WithSummary("Prometheus-compatible metrics")
        .WithDescription("Returns metrics in Prometheus exposition format. Use this endpoint for monitoring and observability. Metrics include API request counts, model inference statistics, and system resource usage.")
        .WithTags("Metrics")
        .Produces(StatusCodes.Status200OK);
    }
}
