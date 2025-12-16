using LlamaApi.Core.Configuration;
using Microsoft.Extensions.Options;

namespace LlamaApi.Infrastructure.Integration.External;

public class HuggingFaceClient(
    IHttpClientFactory httpClientFactory,
    IOptions<HuggingFaceSettings> hfSettings,
    ILogger<HuggingFaceClient> logger) : IHuggingFaceClient
{
    private readonly HttpClient _httpClient = CreateHttpClient(httpClientFactory);
    private readonly HuggingFaceSettings _hfSettings = hfSettings.Value;
    private readonly ILogger<HuggingFaceClient> _logger = logger;

    private static HttpClient CreateHttpClient(IHttpClientFactory factory)
    {
        var client = factory.CreateClient("DownloadService");
        client.Timeout = TimeSpan.FromHours(2);
        return client;
    }

    public void AddHeaders(HttpRequestMessage request)
    {
        // Add User-Agent to avoid blocking
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        
        // Add HuggingFace token if configured
        if (_hfSettings.UseToken && !string.IsNullOrEmpty(_hfSettings.ApiToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _hfSettings.ApiToken);
            _logger.LogDebug("Using HuggingFace API token for authentication");
        }
    }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        AddHeaders(request);
        return _httpClient.SendAsync(request, cancellationToken);
    }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken = default)
    {
        AddHeaders(request);
        return _httpClient.SendAsync(request, completionOption, cancellationToken);
    }
}
