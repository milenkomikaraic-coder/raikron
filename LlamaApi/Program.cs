using LlamaApi.Services.LLMs;
using LlamaApi.Services.Download;
using LlamaApi.Services.Jobs;
using LlamaApi.Services.Sessions;
using LlamaApi.Services.Auth;
using LlamaApi.Services.Hardware;
using LlamaApi.Services.Observability;
using LlamaApi.Middleware;
using LlamaApi.Api.DTOs.Requests;
using LlamaApi.Api.Endpoints;
using LlamaApi.Core.Constants;
using LlamaApi.Core.Configuration;
using LlamaApi.Core.Domain;
using LlamaApi.Infrastructure.Data.Store.FileSystem;
using LlamaApi.Infrastructure.Data.Store.Database.Factories;
using LlamaApi.Infrastructure.Data.Store.Database.Repositories;
using LlamaApi.Infrastructure.Runtime.Health;
using LlamaApi.Infrastructure.Workers.Download;
using Microsoft.Data.Sqlite;
using Serilog;
using System.Diagnostics.Metrics;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.File(Paths.GetLogPath(Directory.GetCurrentDirectory()), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Llama API",
        Version = "v1",
        Description = ".NET 9 Minimal API for local llama.cpp inference"
    });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Services
builder.Services.AddHttpClient("DownloadService", client =>
{
    client.Timeout = TimeSpan.FromHours(2);
});
builder.Services.AddSingleton<HardwareDetectionService>();
builder.Services.AddSingleton<IModelRepository, ModelRepository>();
builder.Services.AddSingleton<ModelRegistryService>();
builder.Services.AddSingleton<ModelManagerService>();
builder.Services.AddSingleton<DownloadService>();
builder.Services.AddSingleton<IJobRepository, JobRepository>();
builder.Services.AddSingleton<JobService>();
builder.Services.AddSingleton<SessionService>();
builder.Services.AddSingleton<JwtAuthService>(sp => new JwtAuthService(sp.GetRequiredService<IOptions<JwtSettings>>()));
builder.Services.AddSingleton<MetricsService>();
builder.Services.AddMetrics();
builder.Services.AddSingleton<LlamaApi.Infrastructure.Integration.External.IHuggingFaceClient, LlamaApi.Infrastructure.Integration.External.HuggingFaceClient>();
builder.Services.AddSingleton<ModelCatalogService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<ApiHealthCheck>("api")
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<ModelsHealthCheck>("models");
// Register Worker as both singleton (for injection) and hosted service (for execution)
builder.Services.AddSingleton<DownloadWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DownloadWorker>());

// Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<DefaultModelSettings>(builder.Configuration.GetSection("DefaultModel"));
builder.Services.Configure<HuggingFaceSettings>(builder.Configuration.GetSection("HuggingFace"));

// Infrastructure
builder.Services.AddSingleton<IFileSystem, FileSystem>();

// SQLite - Create database and tables
await LlamaApi.CreateDatabase.CreateAsync(Directory.GetCurrentDirectory());

var fileSystem = new FileSystem();
builder.Services.AddSingleton<SqliteConnectionFactory>(sp => new SqliteConnectionFactory(Directory.GetCurrentDirectory()));

// Ensure directories exist
fileSystem.CreateDirectory(Paths.GetModelsDirectory(Directory.GetCurrentDirectory()));
fileSystem.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), Paths.LogsDirectory));

var app = builder.Build();

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    
    // Get dev token for auto-authorization
    var jwtServiceForSwagger = app.Services.GetRequiredService<JwtAuthService>();
    var devTokenForSwagger = jwtServiceForSwagger.GetDevToken();
    
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Llama API v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
        
        // Auto-authorize with dev token in development
        if (!string.IsNullOrEmpty(devTokenForSwagger))
        {
            // Inject JavaScript to automatically set the authorization header
            c.InjectJavascript("/swagger-ui-auth.js");
        }
    });
    
    // Serve the auto-auth JavaScript file in development
    if (!string.IsNullOrEmpty(devTokenForSwagger))
    {
        app.MapGet("/swagger-ui-auth.js", () =>
        {
            var script = $@"
                (function() {{
                    function autoAuthorize() {{
                        const token = '{devTokenForSwagger}';
                        if (!token) return;
                        
                        // Try multiple selectors for the authorize button
                        const authBtn = document.querySelector('button.authorize') || 
                                       document.querySelector('button[aria-label*=""Authorize""]') ||
                                       document.querySelector('.btn.authorize');
                        
                        if (authBtn && !authBtn.disabled) {{
                            authBtn.click();
                            
                            // Wait for the modal to open and input to appear
                            setTimeout(function() {{
                                const input = document.querySelector('input[placeholder*=""Bearer""]') || 
                                              document.querySelector('input[placeholder*=""token""]') ||
                                              document.querySelector('input[type=""text""]') ||
                                              document.querySelector('input[type=""password""]');
                                
                                if (input) {{
                                    input.value = 'Bearer ' + token;
                                    input.dispatchEvent(new Event('input', {{ bubbles: true }}));
                                    input.dispatchEvent(new Event('change', {{ bubbles: true }}));
                                    
                                    // Click authorize/close button
                                    setTimeout(function() {{
                                        const authorizeBtn = document.querySelector('.btn-done') ||
                                                           document.querySelector('button[class*=""authorize""]') ||
                                                           document.querySelector('button[aria-label*=""Authorize""]') ||
                                                           document.querySelector('.modal-close');
                                        if (authorizeBtn) {{
                                            authorizeBtn.click();
                                        }}
                                    }}, 200);
                                }}
                            }}, 300);
                        }}
                    }}
                    
                    // Try immediately and also on load
                    if (document.readyState === 'loading') {{
                        document.addEventListener('DOMContentLoaded', function() {{
                            setTimeout(autoAuthorize, 1000);
                        }});
                    }} else {{
                        setTimeout(autoAuthorize, 1000);
                    }}
                    
                    // Also try after a longer delay in case Swagger UI loads slowly
                    setTimeout(autoAuthorize, 2000);
                }})();
            ";
            return Results.Content(script, "application/javascript");
        });
    }
}

// Initialize services
var jwtService = app.Services.GetRequiredService<JwtAuthService>();
jwtService.SeedDevToken();
var devToken = jwtService.GetDevToken();
Console.WriteLine($"Dev JWT Token: {devToken}");

var hardwareService = app.Services.GetRequiredService<HardwareDetectionService>();
hardwareService.DetectHardware();

var registryService = app.Services.GetRequiredService<ModelRegistryService>();
await registryService.InitializeAsync();

var jobService = app.Services.GetRequiredService<JobService>();
await jobService.InitializeAsync();

// Initialize default model (Qwen Coder 7B)
var downloadService = app.Services.GetRequiredService<DownloadService>();
var modelManagerService = app.Services.GetRequiredService<ModelManagerService>();
var defaultModelSettings = app.Services.GetRequiredService<IOptions<DefaultModelSettings>>().Value;
var defaultModelId = defaultModelSettings.ModelId;
var defaultModelSource = defaultModelSettings.Source;
var autoDownload = defaultModelSettings.AutoDownload;
var autoLoad = defaultModelSettings.AutoLoad;

var defaultModelPath = Paths.GetModelPath(Directory.GetCurrentDirectory(), defaultModelId);
var fs = app.Services.GetRequiredService<IFileSystem>();

if (!fs.FileExists(defaultModelPath) && autoDownload)
{
    Console.WriteLine($"Default model '{defaultModelId}' not found. Downloading from {defaultModelSource}...");
    try
    {
        var downloadResult = await downloadService.DownloadModelAsync(defaultModelId, defaultModelSource, null, null);
        
        if (downloadResult.IsAsync && downloadResult.JobId != null)
        {
            Console.WriteLine($"Model download started asynchronously. Job ID: {downloadResult.JobId}");
            Console.WriteLine("Waiting for download to complete...");
            
            // Poll for download completion (with timeout)
            var maxWaitTime = TimeSpan.FromHours(2);
            var pollInterval = TimeSpan.FromSeconds(5);
            var startTime = DateTime.UtcNow;
            var jobId = downloadResult.JobId;
            
            while (DateTime.UtcNow - startTime < maxWaitTime)
            {
                await Task.Delay(pollInterval);
                var job = await jobService.GetJobAsync(jobId);
                
                if (job == null)
                {
                    Console.WriteLine("Warning: Download job not found. Continuing startup...");
                    break;
                }
                
                if (job.Status == JobStatusEnum.Succeeded)
                {
                    Console.WriteLine($"Default model '{defaultModelId}' downloaded successfully.");
                    break;
                }
                else if (job.Status == JobStatusEnum.Failed)
                {
                    Console.WriteLine($"Error: Download failed - {job.Error}");
                    break;
                }
                
                // Show progress
                if (job.Progress > 0)
                {
                    Console.Write($"\rDownload progress: {job.Progress:P0}...");
                }
            }
            
            // Check if file exists now
            if (fs.FileExists(defaultModelPath) && autoLoad)
            {
                Console.WriteLine($"\nLoading default model '{defaultModelId}'...");
                var loadResult = await modelManagerService.LoadModelAsync(defaultModelId, null);
                if (loadResult.Success)
                {
                    Console.WriteLine($"Default model '{defaultModelId}' loaded and set as active.");
                }
                else
                {
                    Console.WriteLine($"Warning: Failed to load default model: {loadResult.ErrorMessage}");
                }
            }
            else if (!fs.FileExists(defaultModelPath))
            {
                Console.WriteLine($"\nWarning: Model download may still be in progress. Use GET /jobs/{jobId} to check status.");
            }
        }
        else
        {
            Console.WriteLine($"Default model '{defaultModelId}' downloaded successfully.");
            
            if (autoLoad)
            {
                Console.WriteLine($"Loading default model '{defaultModelId}'...");
                var loadResult = await modelManagerService.LoadModelAsync(defaultModelId, null);
                if (loadResult.Success)
                {
                    Console.WriteLine($"Default model '{defaultModelId}' loaded and set as active.");
                }
                else
                {
                    Console.WriteLine($"Warning: Failed to load default model: {loadResult.ErrorMessage}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error downloading default model: {ex.Message}");
        Log.Warning(ex, "Failed to download default model during startup");
    }
}
else if (fs.FileExists(defaultModelPath) && autoLoad)
{
    // Model exists, check if it's loaded
    if (modelManagerService.GetActiveModelId() != defaultModelId)
    {
        Console.WriteLine($"Loading default model '{defaultModelId}'...");
        var loadResult = await modelManagerService.LoadModelAsync(defaultModelId, null);
        if (loadResult.Success)
        {
            Console.WriteLine($"Default model '{defaultModelId}' loaded and set as active.");
        }
        else
        {
            Console.WriteLine($"Warning: Failed to load default model: {loadResult.ErrorMessage}");
        }
    }
    else
    {
        Console.WriteLine($"Default model '{defaultModelId}' is already loaded and active.");
    }
}

// Middleware
app.UseMiddleware<JwtAuthMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// Endpoints
app.MapHealthEndpoints();
app.MapModelsEndpoints();
app.MapJobsEndpoints();
app.MapSessionsEndpoints();
app.MapChatEndpoints();

// Configure URL for server ready logging (for VS Code serverReadyAction to auto-open browser)
var listenUrl = "http://localhost:5000";
if (!app.Urls.Any())
{
    app.Urls.Add(listenUrl);
}
else
{
    listenUrl = app.Urls.First();
}

// Log URL when server is actually ready (triggers VS Code serverReadyAction)
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(() =>
{
    // This exact message format triggers VS Code's serverReadyAction pattern matching
    Console.WriteLine($"Now listening on: {listenUrl}");
    System.Diagnostics.Debug.WriteLine($"Server ready at: {listenUrl}");
});

app.Run();
