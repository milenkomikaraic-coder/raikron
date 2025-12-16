using System.Text.Json;
using LlamaApi.Core.Constants;
using LlamaApi.Infrastructure.Integration.External;

namespace LlamaApi.Services.LLMs;

public class ModelCatalogService(
    IHuggingFaceClient hfClient,
    ILogger<ModelCatalogService> logger)
{
    private readonly IHuggingFaceClient _hfClient = hfClient;
    private readonly ILogger<ModelCatalogService> _logger = logger;
    private List<CatalogModel>? _cachedCatalog;
    private DateTime? _cacheTimestamp;

    public async Task<List<CatalogModel>> GetCatalogAsync(bool forceRefresh = false)
    {
        // Return cached catalog if still valid
        if (!forceRefresh && _cachedCatalog != null && _cacheTimestamp.HasValue)
        {
            if (DateTime.UtcNow - _cacheTimestamp.Value < Thresholds.CatalogCacheExpiry)
            {
                _logger.LogDebug("Returning cached catalog");
                return _cachedCatalog;
            }
        }

        _logger.LogInformation("Building model catalog from HuggingFace...");
        var catalog = new List<CatalogModel>();

        // Curated list of popular coding models
        var modelsToCheck = new[]
        {
            // Qwen Coder models
            new { Org = "tensorblock", Repo = "Qwen2.5-Coder-7B-Instruct-GGUF", Category = "Coding", Description = "Qwen 2.5 Coder 7B - Excellent for code generation and understanding" },
            new { Org = "prithivMLmods", Repo = "Qwen2.5-Coder-7B-Instruct-GGUF", Category = "Coding", Description = "Qwen 2.5 Coder 7B (alternative source)" },
            new { Org = "Qwen", Repo = "Qwen2.5-Coder-7B-Instruct", Category = "Coding", Description = "Qwen 2.5 Coder 7B (original, may need conversion)" },
            
            // DeepSeek Coder models
            new { Org = "TheBloke", Repo = "deepseek-coder-6.7B-instruct-GGUF", Category = "Coding", Description = "DeepSeek Coder 6.7B Instruct - Strong coding capabilities" },
            new { Org = "TheBloke", Repo = "deepseek-coder-1.3B-instruct-GGUF", Category = "Coding", Description = "DeepSeek Coder 1.3B Instruct - Lightweight coding model" },
            new { Org = "tensorblock", Repo = "deepseek-coder-6.7b-instruct-GGUF", Category = "Coding", Description = "DeepSeek Coder 6.7B (alternative source)" },
            
            // CodeLlama models
            new { Org = "TheBloke", Repo = "CodeLlama-7B-Instruct-GGUF", Category = "Coding", Description = "CodeLlama 7B Instruct - Meta's coding model" },
            new { Org = "TheBloke", Repo = "CodeLlama-13B-Instruct-GGUF", Category = "Coding", Description = "CodeLlama 13B Instruct - Larger coding model" },
            new { Org = "TheBloke", Repo = "CodeLlama-34B-Instruct-GGUF", Category = "Coding", Description = "CodeLlama 34B Instruct - High performance coding model" },
            
            // StarCoder models
            new { Org = "TheBloke", Repo = "starcoder-GGUF", Category = "Coding", Description = "StarCoder - BigCode's 15B parameter coding model" },
            new { Org = "TheBloke", Repo = "starcoder2-15B-instruct-GGUF", Category = "Coding", Description = "StarCoder2 15B Instruct - Improved StarCoder" },
            
            // WizardCoder models
            new { Org = "TheBloke", Repo = "WizardCoder-Python-7B-V1.0-GGUF", Category = "Coding", Description = "WizardCoder Python 7B - Specialized for Python" },
            new { Org = "TheBloke", Repo = "WizardCoder-Python-13B-V1.0-GGUF", Category = "Coding", Description = "WizardCoder Python 13B - Larger Python model" },
            
            // GPT-OSS models
            new { Org = "TheBloke", Repo = "gpt4all-j-v1.3-groovy-GGUF", Category = "General", Description = "GPT4All-J - Open source GPT alternative" },
            new { Org = "TheBloke", Repo = "GPT4All-13B-snoozy-GGUF", Category = "General", Description = "GPT4All 13B Snoozy - Conversational model" },
            
            // Other coding models
            new { Org = "TheBloke", Repo = "Phind-CodeLlama-34B-v2-GGUF", Category = "Coding", Description = "Phind CodeLlama 34B v2 - Fine-tuned for coding" },
            new { Org = "TheBloke", Repo = "WizardLM-2-7B-Coder-GGUF", Category = "Coding", Description = "WizardLM 2 7B Coder - WizardLM coding variant" },
        };

        var tasks = modelsToCheck.Select(m => CheckModelAvailabilityAsync(m.Org, m.Repo, m.Category, m.Description));
        var results = await Task.WhenAll(tasks);

        catalog.AddRange(results.Where(r => r != null)!);
        
        // Sort by category, then by popularity (size/name) - materialize once at the end
        catalog = catalog
            .OrderBy(m => m.Category)
            .ThenByDescending(m => m.RecommendedQuantization?.FileSizeBytes ?? 0)
            .ToList();

        _cachedCatalog = catalog;
        _cacheTimestamp = DateTime.UtcNow;

        _logger.LogInformation($"Catalog built: {catalog.Count} models available");
        return catalog;
    }

    private async Task<CatalogModel?> CheckModelAvailabilityAsync(string org, string repo, string category, string description)
    {
        try
        {
            _logger.LogDebug($"Checking {org}/{repo}...");
            
            // Check if repository exists and get file list
            var apiUrl = $"https://huggingface.co/api/models/{org}/{repo}/tree/main";
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            var response = await _hfClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug($"Repository {org}/{repo} not accessible: {response.StatusCode}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<HfCatalogFileInfo[]>(content);
            
            if (files == null || files.Length == 0)
            {
                return null;
            }

            // Find GGUF files - materialize once
            var ggufFiles = files
                .Where(f => f.Path.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (ggufFiles.Count == 0)
            {
                _logger.LogDebug($"No GGUF files found in {org}/{repo}");
                return null;
            }

            // Test if files are downloadable (HEAD request)
            var downloadableFiles = new List<CatalogFile>();
            foreach (var file in ggufFiles.Take(10)) // Limit to first 10 to avoid too many requests
            {
                var downloadUrl = $"https://huggingface.co/{org}/{repo}/resolve/main/{file.Path}";
                if (await TestDownloadAsync(downloadUrl))
                {
                    downloadableFiles.Add(new CatalogFile
                    {
                        FileName = file.Path,
                        FileSizeBytes = file.Size,
                        DownloadUrl = downloadUrl,
                        Quantization = ExtractQuantization(file.Path)
                    });
                }
            }

            if (downloadableFiles.Count == 0)
            {
                return null;
            }

            // Find recommended quantization (prefer Q4_K_M, then Q4_0, then smallest reasonable)
            var recommended = downloadableFiles
                .FirstOrDefault(f => f.Quantization?.Contains("Q4_K_M", StringComparison.OrdinalIgnoreCase) == true)
                ?? downloadableFiles.FirstOrDefault(f => f.Quantization?.Contains("Q4_0", StringComparison.OrdinalIgnoreCase) == true)
                ?? downloadableFiles.FirstOrDefault(f => f.Quantization?.Contains("Q4", StringComparison.OrdinalIgnoreCase) == true)
                ?? downloadableFiles.OrderBy(f => f.FileSizeBytes).FirstOrDefault();

            return new CatalogModel
            {
                ModelId = $"{org}-{repo.ToLower().Replace("-gguf", "").Replace("_", "-")}",
                Repository = $"{org}/{repo}",
                Category = category,
                Description = description,
                AvailableFiles = downloadableFiles.Count,
                RecommendedQuantization = recommended,
                AllQuantizations = downloadableFiles.Select(f => f.Quantization ?? "unknown").Distinct().ToArray(),
                Source = $"hf://{org}/{repo}/{recommended?.FileName ?? downloadableFiles.First().FileName}",
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to check {org}/{repo}");
            return null;
        }
    }

    private async Task<bool> TestDownloadAsync(string downloadUrl)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Head, downloadUrl);
            var response = await _hfClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private string? ExtractQuantization(string fileName)
    {
        // Extract quantization from filename patterns like Q4_K_M, Q8_0, etc.
        var patterns = new[] { "Q8_0", "Q6_K", "Q5_K_M", "Q5_K_S", "Q5_0", "Q4_K_M", "Q4_K_S", "Q4_0", "Q3_K_M", "Q3_K_S", "Q2_K", "F16" };
        foreach (var pattern in patterns)
        {
            if (fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return pattern;
            }
        }
        return null;
    }
}

public class HfCatalogFileInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("size")]
    public long Size { get; set; }
}

public class CatalogModel
{
    public string ModelId { get; set; } = "";
    public string Repository { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public int AvailableFiles { get; set; }
    public CatalogFile? RecommendedQuantization { get; set; }
    public string[] AllQuantizations { get; set; } = Array.Empty<string>();
    public string Source { get; set; } = "";
    public DateTime LastChecked { get; set; }
}

public class CatalogFile
{
    public string FileName { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public string DownloadUrl { get; set; } = "";
    public string? Quantization { get; set; }
}
