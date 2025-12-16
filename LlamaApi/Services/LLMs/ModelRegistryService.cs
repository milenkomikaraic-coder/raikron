using Microsoft.Data.Sqlite;
using LlamaApi.Core.Domain;
using LlamaApi.Infrastructure.Data.Store.Database.Factories;
using LlamaApi.Infrastructure.Data.Store.Database.Repositories;

namespace LlamaApi.Services.LLMs;

public class ModelRegistryService(IModelRepository repository, SqliteConnectionFactory connectionFactory)
{
    private readonly IModelRepository _repository = repository;
    private readonly SqliteConnection _db = connectionFactory.CreateConnection();

    public async Task InitializeAsync()
    {
        await _db.OpenAsync();
        var cmd = _db.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS download_models (
                modelId TEXT PRIMARY KEY,
                source TEXT,
                sizeBytes INTEGER,
                status TEXT,
                onDisk INTEGER,
                active INTEGER
            )";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<ModelEntry>> GetAllModelsAsync() => await _repository.GetAllAsync();

    public async Task UpsertModelAsync(string modelId, string? source, long sizeBytes, ModelStatus status, bool onDisk, bool active)
        => await _repository.UpsertAsync(modelId, source, sizeBytes, status, onDisk, active);

    public async Task SetActiveAsync(string modelId) => await _repository.SetActiveAsync(modelId);
}
