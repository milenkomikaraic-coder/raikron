using System.Text;
using LLama;
using LLama.Common;
using LlamaApi.Services.LLMs;
using LlamaApi.Services.Hardware;
using LlamaApi.Services.Sessions;
using LlamaApi.Api.DTOs.Requests;
using LlamaApi.Core.Constants;
using LlamaApi.Core.Domain;
using LlamaApi.Infrastructure.Data.Store.FileSystem;

namespace LlamaApi.Services.LLMs;

public class ModelManagerService(ModelRegistryService registry, HardwareDetectionService hardware, IFileSystem fileSystem)
{
    private readonly ModelRegistryService _registry = registry;
    private readonly HardwareDetectionService _hardware = hardware;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly Dictionary<string, (LLamaWeights weights, ModelParams @params)> _loadedModels = new();
    private string? _activeModelId;

    public string? GetActiveModelId() => _activeModelId;

    public async Task<LoadResult> LoadModelAsync(string modelId, LoadRequest? request)
    {
        var modelPath = Paths.GetModelPath(Directory.GetCurrentDirectory(), modelId);
        if (!_fileSystem.FileExists(modelPath))
            return new LoadResult { Success = false, ErrorCode = "not_found", ErrorMessage = "Model file not found" };

        if (_loadedModels.ContainsKey(modelId))
            return new LoadResult { Success = false, ErrorCode = "conflict", ErrorMessage = "Model already loaded" };

        try
        {
            var (nGpuLayers, nCtx, batch) = _hardware.GetHeuristicDefaults();
            var nCtxFinal = request?.NCtx ?? nCtx;
            var batchFinal = request?.Batch ?? batch;
            var nGpuLayersFinal = request?.NGpuLayers ?? nGpuLayers;

            int gpuLayerCount = nGpuLayersFinal == int.MaxValue ? int.MaxValue : Math.Max(0, nGpuLayersFinal);
            var @params = new ModelParams(modelPath)
            {
                ContextSize = (uint)nCtxFinal,
                BatchSize = (uint)batchFinal,
                GpuLayerCount = gpuLayerCount
            };

            var weights = LLamaWeights.LoadFromFile(@params);
            _loadedModels[modelId] = (weights, @params);
            _activeModelId = modelId;
            await _registry.UpsertModelAsync(modelId, null, 0, ModelStatus.Loaded, true, true);

            return new LoadResult
            {
                Success = true,
                Params = new { nCtx = nCtxFinal, batch = batchFinal, nGpuLayers = nGpuLayersFinal }
            };
        }
        catch (Exception ex)
        {
            return new LoadResult { Success = false, ErrorCode = "load_failed", ErrorMessage = ex.Message };
        }
    }

    public async Task UnloadModelAsync(string modelId)
    {
        if (_loadedModels.TryGetValue(modelId, out var model))
        {
            model.weights.Dispose();
            _loadedModels.Remove(modelId);
        }
        if (_activeModelId == modelId)
            _activeModelId = null;
        await _registry.UpsertModelAsync(modelId, null, 0, ModelStatus.Available, true, false);
    }

    public async Task<bool> SetActiveModelAsync(string modelId)
    {
        if (!_loadedModels.ContainsKey(modelId))
            return false;
        _activeModelId = modelId;
        await _registry.SetActiveAsync(modelId);
        return true;
    }

    public async Task<ChatResult> ChatAsync(string modelId, ChatRequest request, SessionService sessions)
    {
        if (!_loadedModels.TryGetValue(modelId, out var model))
            return new ChatResult { Success = false, ErrorCode = "not_loaded", ErrorMessage = "Model not loaded" };

        // Get or create session if sessionId is provided
        if (!string.IsNullOrEmpty(request.SessionId))
        {
            await sessions.GetOrCreateSessionAsync(request.SessionId);
        }

        try
        {
            // Detect model type based on modelId
            bool isQwenModel = modelId.Contains("qwen", StringComparison.OrdinalIgnoreCase);
            
            string prompt;
            
            if (request.Messages != null && request.Messages.Length > 0)
            {
                var sb = new StringBuilder();
                
                if (isQwenModel)
                {
                    // Qwen chat template: <|im_start|>role\ncontent<|im_end|>
                    var systemMsg = request.Messages.FirstOrDefault(m => m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));
                    if (systemMsg != null)
                    {
                        sb.Append("<|im_start|>system\n").Append(systemMsg.Content).Append("<|im_end|>\n");
                    }
                    else
                    {
                        sb.Append("<|im_start|>system\nYou are a helpful assistant.<|im_end|>\n");
                    }
                    
                    var conversationMsgs = request.Messages.Where(m => !m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));
                    foreach (var msg in conversationMsgs)
                    {
                        var role = msg.Role.ToLower();
                        if (role == "user" || role == "assistant")
                        {
                            sb.Append("<|im_start|>").Append(role).Append("\n").Append(msg.Content).Append("<|im_end|>\n");
                        }
                    }
                    
                    // Always end with assistant prompt
                    sb.Append("<|im_start|>assistant\n");
                }
                else
                {
                    // TinyLlama chat template: <|system|>content</s><|user|>content</s><|assistant|>
                    var systemMsg = request.Messages.FirstOrDefault(m => m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));
                    if (systemMsg != null)
                    {
                        sb.Append("<|system|>\n").Append(systemMsg.Content).Append("</s>");
                    }
                    else
                    {
                        sb.Append("<|system|>\nYou are a helpful assistant.</s>");
                    }
                    
                    var conversationMsgs = request.Messages.Where(m => !m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));
                    foreach (var msg in conversationMsgs)
                    {
                        if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                        {
                            sb.Append("<|user|>\n").Append(msg.Content).Append("</s>");
                        }
                        else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                        {
                            sb.Append("<|assistant|>\n").Append(msg.Content).Append("</s>");
                        }
                    }
                    
                    sb.Append("<|assistant|>\n");
                }
                
                prompt = sb.ToString();
            }
            else if (!string.IsNullOrEmpty(request.Prompt))
            {
                if (isQwenModel)
                {
                    // Qwen template for simple prompt
                    prompt = $"<|im_start|>system\nYou are a helpful assistant.<|im_end|>\n<|im_start|>user\n{request.Prompt}<|im_end|>\n<|im_start|>assistant\n";
                }
                else
                {
                    // TinyLlama template for simple prompt
                    prompt = $"<|system|>\nYou are a helpful assistant.</s><|user|>\n{request.Prompt}</s><|assistant|>\n";
                }
            }
            else
            {
                return new ChatResult { Success = false, ErrorCode = "validation", ErrorMessage = "Either 'prompt' or 'messages' must be provided" };
            }

            var inferenceParams = new InferenceParams
            {
                MaxTokens = request.MaxTokens,
                Temperature = (float)request.Temperature,
                TopP = (float)request.TopP,
                AntiPrompts = request.Stop?.ToList() ?? new List<string>()
            };

            // Use StatelessExecutor for inference with InferAsync
            var executor = new StatelessExecutor(model.weights, model.@params);
            
            var tokens = new List<string>();
            await foreach (var token in executor.InferAsync(prompt, inferenceParams))
            {
                tokens.Add(token);
            }

            var result = string.Join("", tokens);

            return new ChatResult
            {
                Success = true,
                Tokens = result,
                Usage = new { prompt_tokens = prompt.Split().Length, completion_tokens = tokens.Count },
                Timings = new { total_ms = 0 }
            };
        }
        catch (Exception ex)
        {
            return new ChatResult { Success = false, ErrorCode = "chat_failed", ErrorMessage = ex.Message };
        }
    }
}

public class LoadResult
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public object? Params { get; set; }
}

public class ChatResult
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string Tokens { get; set; } = "";
    public object? Usage { get; set; }
    public object? Timings { get; set; }
}
