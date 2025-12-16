using Microsoft.Data.Sqlite;
using LlamaApi.Core.Domain;
using LlamaApi.Infrastructure.Data.Store.Database.Factories;
using LlamaApi.Infrastructure.Data.Store.Database.Repositories;

namespace LlamaApi.Services.Jobs;

public class JobService(IJobRepository repository, SqliteConnectionFactory connectionFactory)
{
    private readonly IJobRepository _repository = repository;
    private readonly SqliteConnection _db = connectionFactory.CreateConnection();

  public async Task InitializeAsync()
    {
        await _db.OpenAsync();
        var cmd = _db.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS download_jobs (
                jobId TEXT PRIMARY KEY,
                status TEXT,
                progress REAL,
                error TEXT
            )";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task CreateJobAsync(string jobId, JobStatusEnum status) => await _repository.CreateAsync(jobId, status);

    public async Task UpdateJobAsync(string jobId, JobStatusEnum status, double progress, string? error = null)
        => await _repository.UpdateAsync(jobId, status, progress, error);

    public async Task<Job?> GetJobAsync(string jobId) => await _repository.GetByIdAsync(jobId);
}
