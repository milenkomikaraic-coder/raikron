namespace LlamaApi.Core.Configuration;

public class DefaultModelSettings
{
    public string ModelId { get; set; } = "qwen-coder-7b";
    public string Source { get; set; } = "hf://TheBloke/Qwen2.5-Coder-7B-Instruct-GGUF/qwen2.5-coder-7b-instruct.Q4_K_M.gguf";
    public bool AutoDownload { get; set; } = true;
    public bool AutoLoad { get; set; } = true;
}
