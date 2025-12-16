namespace LlamaApi.Core.Configuration;

public class HuggingFaceSettings
{
    public string? ApiToken { get; set; }
    public bool UseToken { get; set; } = true;
}
