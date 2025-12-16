namespace LlamaApi.Infrastructure.Integration.External;

public interface IHuggingFaceClient
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken = default);
    void AddHeaders(HttpRequestMessage request);
}
