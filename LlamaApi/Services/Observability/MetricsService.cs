using System.Diagnostics.Metrics;

namespace LlamaApi.Services.Observability;

public class MetricsService
{
    private readonly Meter _meter;
    private readonly Counter<long> _requestsTotal;
    private readonly Counter<long> _chatRequestsTotal;
    private readonly Counter<long> _downloadRequestsTotal;
    private readonly Histogram<double> _chatDuration;
    private readonly Histogram<double> _downloadDuration;
    private long _modelsLoadedCount;
    private long _modelsOnDiskCount;

    public MetricsService()
    {
        _meter = new Meter("LlamaApi", "1.0.0");
        _requestsTotal = _meter.CreateCounter<long>("requests_total", "Total API requests");
        _chatRequestsTotal = _meter.CreateCounter<long>("chat_requests_total", "Total chat completion requests");
        _downloadRequestsTotal = _meter.CreateCounter<long>("download_requests_total", "Total model download requests");
        _chatDuration = _meter.CreateHistogram<double>("chat_duration_seconds", "seconds", "Chat completion duration");
        _downloadDuration = _meter.CreateHistogram<double>("download_duration_seconds", "seconds", "Model download duration");
        
        // Observable gauges that read from fields
        _meter.CreateObservableGauge("models_loaded", () => _modelsLoadedCount, "Number of models currently loaded in memory");
        _meter.CreateObservableGauge("models_on_disk", () => _modelsOnDiskCount, "Number of models available on disk");
    }

    public void IncrementRequestsTotal() => _requestsTotal.Add(1);
    public void IncrementChatRequests() => _chatRequestsTotal.Add(1);
    public void IncrementDownloadRequests() => _downloadRequestsTotal.Add(1);
    public void RecordChatDuration(double seconds) => _chatDuration.Record(seconds);
    public void RecordDownloadDuration(double seconds) => _downloadDuration.Record(seconds);
    public void SetModelsLoaded(long count) => _modelsLoadedCount = count;
    public void SetModelsOnDisk(long count) => _modelsOnDiskCount = count;

    public string GetPrometheusText()
    {
        // For now, return a basic Prometheus format
        // In a production system, you'd use prometheus-net.AspNetCore or similar
        // to automatically export metrics in Prometheus format
        return "# HELP requests_total Total API requests\n# TYPE requests_total counter\nrequests_total 0\n" +
               "# HELP chat_requests_total Total chat completion requests\n# TYPE chat_requests_total counter\nchat_requests_total 0\n" +
               "# HELP download_requests_total Total model download requests\n# TYPE download_requests_total counter\ndownload_requests_total 0\n" +
               $"# HELP models_loaded Number of models currently loaded in memory\n# TYPE models_loaded gauge\nmodels_loaded {_modelsLoadedCount}\n" +
               $"# HELP models_on_disk Number of models available on disk\n# TYPE models_on_disk gauge\nmodels_on_disk {_modelsOnDiskCount}\n";
    }
}
