# Llama API - .NET 9 Local Llama API

A .NET 9 Minimal API wrapping local llama.cpp via LLama.NET with hardware-aware defaults (DirectML → CUDA → CPU fallback).

## Requirements

- .NET 9 SDK
- Windows (recommended for best performance)
- GPU support (optional, falls back to CPU)
- LLamaSharp uses CPU backend which can leverage DirectML on Windows if available

## Setup

### Using Cursor (Recommended)

1. **Open in Cursor**: Open the `LlamaApi` folder in Cursor
2. **Install Extensions**: Cursor will prompt to install recommended extensions (C# Dev Kit, etc.)
3. **Restore Packages**: Press `Ctrl+Shift+P` → "Tasks: Run Task" → "restore"
4. **Build**: Press `Ctrl+Shift+B` (or "Tasks: Run Task" → "build")
5. **Debug**: Press `F5` to start debugging, or `Ctrl+F5` to run without debugging

The API will start on `http://localhost:5000` and automatically open in your browser.

**Note**: Cursor is fully compatible with VS Code configurations. All `.vscode` settings work in Cursor.

### Using Command Line

1. Restore dependencies:
   ```bash
   dotnet restore
   ```

2. Build:
   ```bash
   dotnet build
   ```

3. Run:
   ```bash
   dotnet run
   ```

The API will start on `http://localhost:5000` (or the port configured in `launchSettings.json`).

## Swagger UI

Once the API is running, open your browser and navigate to:
- **Swagger UI**: `http://localhost:5000` (root URL)
- **Swagger JSON**: `http://localhost:5000/swagger/v1/swagger.json`

The Swagger UI provides an interactive interface to test all API endpoints. You'll need to:
1. Click the "Authorize" button at the top
2. Enter your JWT token (format: `Bearer YOUR_TOKEN` or just `YOUR_TOKEN`)
3. Click "Authorize" to authenticate
4. Test endpoints directly from the UI

## Authentication

A development JWT token is seeded at startup. To get the token, check the console output or logs.

Example token usage:
```bash
curl -H "Authorization: Bearer YOUR_TOKEN" http://localhost:5000/health
```

## Endpoints

- `GET /health` - Health check with hardware info
- `GET /metrics` - Prometheus metrics
- `GET /models` - List all registered models
- `GET /models/catalog` - Get catalog of available downloadable models (tests availability without downloading)
- `POST /models/download` - Download a model
- `GET /jobs/{id}` - Get download job status
- `POST /models/load` - Load a model
- `POST /models/unload` - Unload a model
- `POST /models/active` - Set active model
- `POST /sessions/{id}/reset` - Reset a session
- `POST /chat` - Chat endpoint (SSE or NDJSON)
- `POST /chat/stream` - Chat endpoint (always SSE)

## Model Storage

Models are stored in `./Infrastructure/Data/LLMs/` directory. Place GGUF model files there with the naming pattern `{modelId}.gguf`.

## Configuration

Edit `appsettings.json` to configure:

### HuggingFace Authentication (Optional)

If you encounter 401 Unauthorized errors when downloading models, you can optionally add a HuggingFace API token:

```json
"HuggingFace": {
  "ApiToken": "your-huggingface-token-here",
  "UseToken": true
}
```

Get your token from: https://huggingface.co/settings/tokens

**Note:** For public repositories, a token is usually not required. The 401 error is often due to rate limiting - wait a few minutes and try again.

## Troubleshooting

### Model Download Fails with 401 Unauthorized

1. **Wait and retry** - HuggingFace may be rate limiting. Wait 5-10 minutes and restart the API.
2. **Add HuggingFace token** - See configuration above (optional, usually not needed for public repos).
3. **Manual download** - Download the model manually and place it in `Infrastructure/Data/LLMs/{modelId}.gguf`, then restart the API.

## Configuration

Edit `appsettings.json` to configure:
- JWT secret key (change in production!)
- Logging levels
- Other settings

## Notes

- The API requires JWT authentication on all endpoints
- Models must be downloaded/placed manually for now (download endpoint is a placeholder)
- Hardware detection runs on startup
- Auto-unload of idle models is not yet implemented (120 min threshold)
