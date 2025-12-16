using Microsoft.Data.Sqlite;
using LlamaApi.Core.Constants;

namespace LlamaApi;

public static class CreateDatabase
{
    public static async Task CreateAsync(string baseDirectory)
    {
        var dbPath = Paths.GetDatabasePath(baseDirectory);
        
        // Ensure directory exists
        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        // Delete existing database if it exists
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }

        // Create database and tables
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=True;Cache=Shared");
        await connection.OpenAsync();

        // Create download_models table
        var createModelsTable = connection.CreateCommand();
        createModelsTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS download_models (
                modelId TEXT PRIMARY KEY,
                source TEXT,
                sizeBytes INTEGER,
                status TEXT,
                onDisk INTEGER,
                active INTEGER
            )";
        await createModelsTable.ExecuteNonQueryAsync();

        // Create download_jobs table
        var createJobsTable = connection.CreateCommand();
        createJobsTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS download_jobs (
                jobId TEXT PRIMARY KEY,
                status TEXT,
                progress REAL,
                error TEXT
            )";
        await createJobsTable.ExecuteNonQueryAsync();

        Console.WriteLine($"Database created successfully at: {dbPath}");
        Console.WriteLine("Tables created: download_models, download_jobs");
    }
}
