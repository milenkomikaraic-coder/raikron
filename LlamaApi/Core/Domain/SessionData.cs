namespace LlamaApi.Core.Domain;

public class SessionData
{
    public string SessionId { get; set; } = "";
    public object? Context { get; set; }
    public object? KvCache { get; set; }
}
