using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LlamaApi.Infrastructure.Runtime.Health;

public class ApiHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic API health check - verify the API is responsive
            // This is a simple check that always passes if the health check endpoint is reachable
            // In a more complex scenario, you might check:
            // - API configuration is valid
            // - Required services are available
            // - API can process requests
            
            return Task.FromResult(HealthCheckResult.Healthy("API is operational"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("API health check failed", ex));
        }
    }
}
