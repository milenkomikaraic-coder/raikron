using LlamaApi.Api.DTOs.Requests;
using LlamaApi.Api.DTOs.Responses;
using LlamaApi.Api.Validation;
using LlamaApi.Services.LLMs;
using LlamaApi.Services.Download;

namespace LlamaApi.Api.Endpoints;

public static class ModelsEndpoints
{
    public static void MapModelsEndpoints(this WebApplication app)
    {
        app.MapGet("/models", async (ModelRegistryService registry) =>
        {
            var models = await registry.GetAllModelsAsync();
            return Results.Ok(new ModelsResponse(models));
        })
        .WithSummary("List all registered models")
        .WithDescription("Returns a list of all models registered in the system, including their status (available, downloading, loaded, error), size, disk presence, and active state. Example response: {\"models\": [{\"modelId\": \"tinyllama\", \"status\": \"available\", \"sizeBytes\": 637000000, \"onDisk\": true, \"active\": false}]}")
        .WithTags("Models")
        .Produces<object>(StatusCodes.Status200OK);

        app.MapGet("/models/catalog", async (ModelCatalogService catalog, bool refresh = false) =>
        {
            var models = await catalog.GetCatalogAsync(refresh);
            return Results.Ok(new CatalogResponse(
                Models: models,
                Count: models.Count,
                Categories: models.GroupBy(m => m.Category).Select(g => new CategoryInfo(g.Key, g.Count())),
                LastUpdated: models.FirstOrDefault()?.LastChecked
            ));
        })
        .WithSummary("Get available model catalog")
        .WithDescription("Returns a catalog of downloadable coding models from HuggingFace. Tests model availability without downloading. Focuses on coding models (Qwen Coder, DeepSeek, CodeLlama, StarCoder, WizardCoder) and popular GPT-OSS models. Use ?refresh=true to force refresh. Example response includes model ID, repository, category, description, recommended quantization, and download source.")
        .WithTags("Models")
        .Produces<object>(StatusCodes.Status200OK);

        app.MapPost("/models/download", async (DownloadService download, DownloadRequest body) =>
        {
            if (body == null)
                return Results.BadRequest(new ErrorResponse(new ErrorDetail("validation", "Request body is required")));
            
            var validationResult = ValidationExtensions.ValidateModel(body);
            if (validationResult != null)
                return validationResult;

            var result = await download.DownloadModelAsync(body.ModelId, body.Source, body.Checksum, body.Priority);
            if (result.IsAsync && result.JobId != null)
                return Results.Accepted($"/jobs/{result.JobId}", new DownloadResponse(Job: new DownloadJobResponse(result.JobId), Model: null));
            
            // Extract model info from anonymous type using JSON serialization
            if (result.Model != null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(result.Model);
                var modelDict = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>>(json);
                if (modelDict != null)
                {
                    var modelInfo = new ModelInfoResponse(
                        ModelId: modelDict.TryGetValue("modelId", out var modelId) ? modelId.GetString() ?? "" : "",
                        Status: modelDict.TryGetValue("status", out var status) ? status.GetString() ?? "available" : "available",
                        SizeBytes: modelDict.TryGetValue("sizeBytes", out var sizeBytes) ? sizeBytes.GetInt64() : 0,
                        OnDisk: modelDict.TryGetValue("onDisk", out var onDisk) && onDisk.GetBoolean(),
                        Active: modelDict.TryGetValue("active", out var active) && active.GetBoolean()
                    );
                    return Results.Ok(new DownloadResponse(Job: null, Model: modelInfo));
                }
            }
            
            return Results.Ok(new DownloadResponse(Job: null, Model: null));
        })
        .WithSummary("Download a model from HuggingFace")
        .WithDescription("Downloads a GGUF model from HuggingFace. For models ≤2GB, returns immediately (200). For larger models, returns a job ID (202) - use GET /jobs/{id} to check progress. Source format: hf://org/model-name or hf://org/model-name/path/to/file.gguf. Example request body (copy-paste ready): {\"modelId\": \"tinyllama\", \"source\": \"hf://TinyLlama/TinyLlama-1.1B-Chat-v1.0\", \"checksum\": null, \"priority\": null}")
        .Accepts<DownloadRequest>("application/json")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status202Accepted)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .WithTags("Models");

        app.MapPost("/models/load", async (ModelManagerService modelMgr, LoadRequest body) =>
        {
            if (body == null)
                return Results.BadRequest(new ErrorResponse(new ErrorDetail("validation", "Request body is required")));
            
            var validationResult = ValidationExtensions.ValidateModel(body);
            if (validationResult != null)
                return validationResult;

            var result = await modelMgr.LoadModelAsync(body.ModelId, body);
            if (!result.Success)
                return Results.UnprocessableEntity(new ErrorResponse(new ErrorDetail(result.ErrorCode ?? "unknown", result.ErrorMessage ?? "Unknown error")));

            return Results.Ok(new LoadResponse(body.ModelId, Loaded: true, result.Params));
        })
        .WithSummary("Load a model into memory")
        .WithDescription("Loads a model into memory for inference. All parameters are optional - if omitted, hardware-aware defaults are used. nCtx: context window size (default: based on VRAM), batch: batch size (default: 512), nGpuLayers: number of layers to offload to GPU (default: auto-detected), offload: \"auto\", \"gpu\", or \"cpu\" (default: auto). Example request body (copy-paste ready): {\"modelId\": \"tinyllama\", \"nCtx\": 2048, \"batch\": 512, \"nGpuLayers\": 0, \"offload\": null}")
        .Accepts<LoadRequest>("application/json")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .Produces<object>(StatusCodes.Status422UnprocessableEntity)
        .WithTags("Models");

        app.MapPost("/models/unload", async (ModelManagerService modelMgr, UnloadRequest? body) =>
        {
            var modelId = body?.ModelId ?? modelMgr.GetActiveModelId();
            if (string.IsNullOrEmpty(modelId))
                return Results.NotFound(new ErrorResponse(new ErrorDetail("not_found", "No active model")));

            await modelMgr.UnloadModelAsync(modelId);
            return Results.Ok(new UnloadResponse(modelId, Unloaded: true));
        })
        .WithSummary("Unload a model from memory")
        .WithDescription("Unloads a model from memory to free resources. If modelId is omitted or null, unloads the currently active model. Example request body (copy-paste ready): {\"modelId\": \"tinyllama\"} or {} to unload active model")
        .Accepts<UnloadRequest>("application/json")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status404NotFound)
        .WithTags("Models");

        app.MapPost("/models/active", async (ModelManagerService modelMgr, ActiveRequest body) =>
        {
            if (body == null)
                return Results.BadRequest(new ErrorResponse(new ErrorDetail("validation", "Request body is required")));
            
            var validationResult = ValidationExtensions.ValidateModel(body);
            if (validationResult != null)
                return validationResult;

            var success = await modelMgr.SetActiveModelAsync(body.ModelId);
            if (!success)
                return Results.NotFound(new ErrorResponse(new ErrorDetail("not_found", "Model not found")));

            return Results.Ok(new ActiveResponse(body.ModelId, Active: true));
        })
        .WithSummary("Set the active model")
        .WithDescription("Sets a loaded model as the active model for inference. The active model is used by default when modelId is omitted in chat requests. Example request body (copy-paste ready): {\"modelId\": \"tinyllama\"}")
        .Accepts<ActiveRequest>("application/json")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .Produces<object>(StatusCodes.Status404NotFound)
        .WithTags("Models");
    }
}
