namespace LlamaApi.Core.Configuration;

public class JwtSettings
{
    public string SecretKey { get; set; } = "dev-secret-key-change-in-production-min-32-chars-required";
}
