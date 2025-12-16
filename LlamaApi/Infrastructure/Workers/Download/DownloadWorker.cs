using System.Collections.Concurrent;
using LlamaApi.Core.Domain;
using LlamaApi.Services.Download;
using LlamaApi.Services.Jobs;

namespace LlamaApi.Infrastructure.Workers.Download;

public class DownloadWorker : BackgroundService
{
    private readonly ConcurrentQueue<DownloadJob> _downloadQueue = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DownloadWorker> _logger;

    public DownloadWorker(IServiceProvider serviceProvider, ILogger<DownloadWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void EnqueueDownload(DownloadJob job)
    {
        _downloadQueue.Enqueue(job);
        _logger.LogInformation($"Download job {job.JobId} enqueued for model {job.ModelId}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_downloadQueue.TryDequeue(out var job))
            {
                try
                {
                    await ProcessDownloadJobAsync(job, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing download job {job.JobId}");
                    // DownloadFileAsync already handles error status updates, but we'll update it here as a fallback
                    await UpdateJobStatusAsync(job.JobId, JobStatusEnum.Failed, 0.0, ex.Message);
                }
            }
            else
            {
                await Task.Delay(100, stoppingToken);
            }
        }
    }

    private async Task ProcessDownloadJobAsync(DownloadJob job, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var downloadService = scope.ServiceProvider.GetRequiredService<DownloadService>();

        // DownloadFileAsync already handles job status updates and registry updates
        await downloadService.DownloadFileAsync(
            job.JobId,
            job.DownloadUrl,
            job.FilePath,
            job.ModelId,
            job.Source,
            job.ExpectedSize,
            job.Checksum
        );
    }

    private async Task UpdateJobStatusAsync(string jobId, JobStatusEnum status, double progress, string? error = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<JobService>();
        await jobService.UpdateJobAsync(jobId, status, progress, error);
    }
}
