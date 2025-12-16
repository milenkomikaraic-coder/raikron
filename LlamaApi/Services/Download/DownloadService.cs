using System.Buffers;
using LlamaApi.Core.Constants;
using LlamaApi.Core.Domain;
using LlamaApi.Services.LLMs;
using LlamaApi.Services.Jobs;
using LlamaApi.Infrastructure.Data.Store.FileSystem;
using LlamaApi.Infrastructure.Integration.External;

namespace LlamaApi.Services.Download;

public class DownloadService(
    ModelRegistryService registry,
    JobService jobs,
    IHuggingFaceClient hfClient,
    ILogger<DownloadService> logger,
    IFileSystem fileSystem,
    LlamaApi.Infrastructure.Workers.Download.DownloadWorker? downloadWorker = null)
{
    private readonly ModelRegistryService _registry = registry;
    private readonly JobService _jobs = jobs;
    private readonly IHuggingFaceClient _hfClient = hfClient;
    private readonly ILogger<DownloadService> _logger = logger;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly LlamaApi.Infrastructure.Workers.Download.DownloadWorker? _downloadWorker = downloadWorker;

    public async Task<DownloadResult> DownloadModelAsync(string modelId, string source, string? checksum, string? priority)
    {
        var modelPath = Paths.GetModelPath(Directory.GetCurrentDirectory(), modelId);
        var exists = _fileSystem.FileExists(modelPath);
        
        if (exists)
        {
            var size = _fileSystem.GetFileSize(modelPath);
            await _registry.UpsertModelAsync(modelId, source, size, ModelStatus.Available, true, false);
            return new DownloadResult { IsAsync = false, Model = new { modelId, status = "available", sizeBytes = size, onDisk = true, active = false } };
        }

        // Parse HuggingFace source
        if (!source.StartsWith("hf://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Source must be in format hf://org/model-name or hf://org/model-name/file.gguf");
        }

        var hfPath = source[5..]; // Remove "hf://"
        var parts = hfPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            throw new ArgumentException("Invalid HuggingFace source format. Expected: hf://org/model-name");
        }

        var org = parts[0];
        var modelName = parts[1];
        string? specifiedFileName = null;
        
        if (parts.Length > 2)
        {
            // Specific file path provided
            specifiedFileName = string.Join("/", parts.Skip(2));
        }

        // Get file info from HuggingFace API - try multiple branches
        long fileSize = 0;
        string? downloadUrl = null;
        string? fileName = null;
        string[] branches = { "main", "master" };
        List<string> triedUrls = new();

        foreach (var branch in branches)
        {
            try
            {
                var fileInfoUrl = $"https://huggingface.co/api/models/{org}/{modelName}/tree/{branch}";
                _logger.LogInformation($"Checking HuggingFace API: {fileInfoUrl}");
                var apiRequest = new HttpRequestMessage(HttpMethod.Get, fileInfoUrl);
                var response = await _hfClient.SendAsync(apiRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var files = System.Text.Json.JsonSerializer.Deserialize<HfFileInfo[]>(content);
                    
                    if (files != null && files.Length > 0)
                    {
                        _logger.LogInformation($"Found {files.Length} files in repository");
                        
                        HfFileInfo? targetFile = null;
                        
                        // If specific file was provided, try to find it
                        if (!string.IsNullOrEmpty(specifiedFileName))
                        {
                            // Try exact match first
                            targetFile = files.FirstOrDefault(f => 
                                f.Path.Equals(specifiedFileName, StringComparison.OrdinalIgnoreCase) ||
                                f.Path.EndsWith(specifiedFileName, StringComparison.OrdinalIgnoreCase) ||
                                f.Path.Contains(specifiedFileName, StringComparison.OrdinalIgnoreCase));
                            
                            if (targetFile != null)
                            {
                                _logger.LogInformation($"Found specified file: {targetFile.Path}");
                            }
                        }
                        
                        // If not found and specific file was provided, try just the filename (without path)
                        if (targetFile == null && !string.IsNullOrEmpty(specifiedFileName))
                        {
                            var justFileName = Path.GetFileName(specifiedFileName);
                            targetFile = files.FirstOrDefault(f => 
                                f.Path.EndsWith(justFileName, StringComparison.OrdinalIgnoreCase));
                        }
                        
                        // If still not found, try any .gguf file
                        if (targetFile == null)
                        {
                            targetFile = files.FirstOrDefault(f => f.Path.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase));
                        }
                        
                        if (targetFile != null)
                        {
                            fileSize = targetFile.Size;
                            fileName = targetFile.Path;
                            
                            // Try to get signed URL from HuggingFace Hub API for large files
                            // This often works better than direct resolve URLs
                            try
                            {
                                var hubApiUrl = $"https://huggingface.co/api/models/{org}/{modelName}/tree/{branch}/{fileName}";
                                var hubRequest = new HttpRequestMessage(HttpMethod.Get, hubApiUrl);
                                var hubResponse = await _hfClient.SendAsync(hubRequest);
                                
                                if (hubResponse.IsSuccessStatusCode)
                                {
                                    var hubContent = await hubResponse.Content.ReadAsStringAsync();
                                    var hubFileInfo = System.Text.Json.JsonSerializer.Deserialize<HfFileInfo>(hubContent);
                                    if (hubFileInfo != null && !string.IsNullOrEmpty(hubFileInfo.DownloadUrl))
                                    {
                                        downloadUrl = hubFileInfo.DownloadUrl;
                                        _logger.LogInformation($"Using signed download URL from Hub API");
                                    }
                                    else
                                    {
                                        downloadUrl = $"https://huggingface.co/{org}/{modelName}/resolve/{branch}/{fileName}";
                                    }
                                }
                                else
                                {
                                    downloadUrl = $"https://huggingface.co/{org}/{modelName}/resolve/{branch}/{fileName}";
                                }
                            }
                            catch
                            {
                                downloadUrl = $"https://huggingface.co/{org}/{modelName}/resolve/{branch}/{fileName}";
                            }
                            
                            _logger.LogInformation($"Found file {fileName} ({fileSize} bytes) in branch {branch}");
                            break;
                        }
                        else
                        {
                            _logger.LogWarning($"No GGUF files found in {org}/{modelName}. Available files: {string.Join(", ", files.Take(10).Select(f => f.Path))}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"No files returned from HuggingFace API for {org}/{modelName}");
                    }
                }
                else
                {
                    _logger.LogWarning($"HuggingFace API returned {response.StatusCode} for {fileInfoUrl}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to get file info from branch {branch}");
            }
        }

        // Fallback: try direct download URL (common patterns)
        if (string.IsNullOrEmpty(downloadUrl))
        {
            var pathsToTry = new List<string>();
            
            // If specific file was provided, try it first
            if (!string.IsNullOrEmpty(specifiedFileName))
            {
                pathsToTry.Add(specifiedFileName);
                // Also try just the filename
                var justFileName = Path.GetFileName(specifiedFileName);
                if (justFileName != specifiedFileName)
                {
                    pathsToTry.Add(justFileName);
                }
            }
            
            // Add common patterns
            pathsToTry.AddRange(new[]
            {
                $"{modelName}.gguf",
                $"ggml-model-{modelName}.gguf"
            });

            foreach (var path in pathsToTry)
            {
                var testUrl = $"https://huggingface.co/{org}/{modelName}/resolve/main/{path}";
                triedUrls.Add(testUrl);
                try
                {
                    _logger.LogInformation($"Trying HEAD request: {testUrl}");
                    var headRequest = new HttpRequestMessage(HttpMethod.Head, testUrl);
                    var headResponse = await _hfClient.SendAsync(headRequest);
                    if (headResponse.IsSuccessStatusCode)
                    {
                        downloadUrl = testUrl;
                        fileSize = headResponse.Content.Headers.ContentLength ?? 0;
                        fileName = path;
                        _logger.LogInformation($"Found file via HEAD request: {path} ({fileSize} bytes)");
                        break;
                    }
                    else
                    {
                        _logger.LogDebug($"HEAD request returned {headResponse.StatusCode} for {testUrl}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, $"HEAD request failed for {testUrl}");
                }
            }
        }

        // Last resort: construct URL directly with specified file or fallback
        if (string.IsNullOrEmpty(downloadUrl))
        {
            var finalFileName = specifiedFileName ?? fileName ?? $"{modelName}.gguf";
            downloadUrl = $"https://huggingface.co/{org}/{modelName}/resolve/main/{finalFileName}";
            triedUrls.Add(downloadUrl);
            fileName = finalFileName;
            _logger.LogWarning($"Using fallback URL: {downloadUrl}");
        }

        // If still no URL found, provide helpful error
        if (string.IsNullOrEmpty(downloadUrl))
        {
            var errorMsg = $"Could not find GGUF file in repository {org}/{modelName}. " +
                          $"This repository may not contain GGUF files. " +
                          $"Try using a GGUF-specific repository like 'TheBloke/{modelName}-GGUF' or 'tensorblock/{modelName}-GGUF'. " +
                          $"Tried URLs: {string.Join(", ", triedUrls)}";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        await _registry.UpsertModelAsync(modelId, source, fileSize, ModelStatus.Downloading, false, false);

        // Sync for small files, async for large
        if (fileSize > 0 && fileSize <= Thresholds.SyncDownloadThresholdBytes)
        {
            try
            {
                await DownloadFileAsync(downloadUrl, modelPath, null, checksum);
                var finalSize = _fileSystem.GetFileSize(modelPath);
                await _registry.UpsertModelAsync(modelId, source, finalSize, ModelStatus.Available, true, false);
                return new DownloadResult 
                { 
                    IsAsync = false, 
                    Model = new { modelId, status = "available", sizeBytes = finalSize, onDisk = true, active = false } 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync download failed");
                await _registry.UpsertModelAsync(modelId, source, fileSize, ModelStatus.Error, false, false);
                throw;
            }
        }
        else
        {
            // Async download - enqueue for background processing
            var jobId = Guid.NewGuid().ToString();
            await _jobs.CreateJobAsync(jobId, JobStatusEnum.Queued);
            
            // Enqueue download for background processing
            if (_downloadWorker != null)
            {
                _downloadWorker.EnqueueDownload(new LlamaApi.Infrastructure.Workers.Download.DownloadJob(
                    JobId: jobId,
                    DownloadUrl: downloadUrl,
                    FilePath: modelPath,
                    ModelId: modelId,
                    Source: source,
                    ExpectedSize: fileSize,
                    Checksum: checksum
                ));
            }
            else
            {
                // Fallback to Task.Run if Worker is not available
                _ = Task.Run(async () => await DownloadFileAsync(jobId, downloadUrl, modelPath, modelId, source, fileSize, checksum));
            }
            
            return new DownloadResult { IsAsync = true, JobId = jobId };
        }
    }

    internal async Task DownloadFileAsync(string? jobId, string downloadUrl, string filePath, string? modelId, string? source, long expectedSize, string? checksum)
    {
        if (jobId != null)
        {
            await _jobs.UpdateJobAsync(jobId, JobStatusEnum.Running, 0.0);
        }

        try
        {
            await DownloadFileAsync(downloadUrl, filePath, jobId, checksum);
            
            var finalSize = _fileSystem.GetFileSize(filePath);
            if (jobId != null && modelId != null && source != null)
            {
                await _jobs.UpdateJobAsync(jobId, JobStatusEnum.Succeeded, 1.0);
                await _registry.UpsertModelAsync(modelId, source, finalSize, ModelStatus.Available, true, false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Async download failed");
            if (jobId != null)
            {
                await _jobs.UpdateJobAsync(jobId, JobStatusEnum.Failed, 0.0, ex.Message);
            }
            if (modelId != null && source != null)
            {
                await _registry.UpsertModelAsync(modelId, source, expectedSize, ModelStatus.Error, false, false);
            }
        }
    }

    private async Task DownloadFileAsync(string downloadUrl, string filePath, string? jobId, string? checksum)
    {
        _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(filePath)!);
        
        _logger.LogInformation($"Starting download from: {downloadUrl}");
        
        // Create request with proper headers
        var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        var response = await _hfClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Download failed with status {response.StatusCode}. URL: {downloadUrl}. Response: {errorContent}");
            
            // Provide more helpful error message
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException($"Unauthorized (401) - This may be due to HuggingFace rate limiting or authentication requirements. Try again later or check if the file exists at: {downloadUrl}");
            }
            
            throw new HttpRequestException($"Failed to download from {downloadUrl}: {response.StatusCode} - {errorContent}");
        }

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var tempPath = filePath + ".tmp";

        using (var fileStream = _fileSystem.CreateFileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var httpStream = await response.Content.ReadAsStreamAsync())
        {
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSizes.DownloadBuffer);
            try
            {
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await httpStream.ReadAsync(buffer, 0, BufferSizes.DownloadBuffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (jobId != null && totalBytes > 0)
                    {
                        var progress = (double)totalRead / totalBytes;
                        await _jobs.UpdateJobAsync(jobId, JobStatusEnum.Running, progress);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        // Verify checksum if provided
        if (!string.IsNullOrEmpty(checksum))
        {
            // TODO: Implement checksum verification
            _logger.LogWarning("Checksum verification not yet implemented");
        }

        // Move temp file to final location
        if (_fileSystem.FileExists(filePath))
        {
            _fileSystem.DeleteFile(filePath);
        }
        _fileSystem.MoveFile(tempPath, filePath);
    }
}

public class HfFileInfo
{
    public string Path { get; set; } = "";
    public long Size { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("downloadUrl")]
    public string? DownloadUrl { get; set; }
}

public class DownloadResult
{
    public bool IsAsync { get; set; }
    public string? JobId { get; set; }
    public object? Model { get; set; }
}
