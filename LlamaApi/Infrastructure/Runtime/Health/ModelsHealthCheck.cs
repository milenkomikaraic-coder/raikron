using Microsoft.Extensions.Diagnostics.HealthChecks;
using LlamaApi.Services.LLMs;

namespace LlamaApi.Infrastructure.Runtime.Health;

public class ModelsHealthCheck(ModelManagerService modelManager) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var activeModel = modelManager.GetActiveModelId();
            if (string.IsNullOrEmpty(activeModel))
            {
                return Task.FromResult(HealthCheckResult.Degraded("No active model loaded"));
            }
            return Task.FromResult(HealthCheckResult.Healthy($"Active model: {activeModel}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Model service check failed", ex));
        }
    }
}
