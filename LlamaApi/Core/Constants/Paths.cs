namespace LlamaApi.Core.Constants;

public static class Paths
{
    public const string LogsDirectory = "logs";
    public const string DatabaseFileName = "RaikronDB.db";
    public const string LogFileName = "llama-api-.log";

    public static string GetDatabasePath(string baseDirectory) => 
        Path.Combine(baseDirectory, "Infrastructure", "Data", "Store", "Database", "Local", DatabaseFileName);

    public static string GetModelPath(string baseDirectory, string modelId) => 
        Path.Combine(baseDirectory, "Infrastructure", "Data", "LLMs", $"{modelId}.gguf");
    
    public static string GetModelsDirectory(string baseDirectory) =>
        Path.Combine(baseDirectory, "Infrastructure", "Data", "LLMs");

    public static string GetLogPath(string baseDirectory) => 
        Path.Combine(baseDirectory, LogsDirectory, LogFileName);
}
