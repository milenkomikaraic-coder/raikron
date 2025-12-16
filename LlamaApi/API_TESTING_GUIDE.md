# API Testing Guide

## Quick Start

1. **Start the API**: `dotnet run` (from `LlamaApi` directory)
2. **Get JWT Token**: Check console output for "Dev JWT Token: ..."
3. **Open Swagger UI**: Navigate to `http://localhost:5000`
4. **Authorize**: Click "Authorize" button, paste your JWT token
5. **Test Endpoints**: Use the JSON examples below or copy from `test-requests/` folder

## JSON Test Files

All test JSON files are in the `test-requests/` directory:

- `01-download-model.json` - Download a model from HuggingFace
- `02-load-model.json` - Load a model into memory
- `03-set-active-model.json` - Set a model as active
- `04-chat-simple.json` - Simple chat with a prompt
- `05-chat-with-messages.json` - Chat with conversation history
- `06-unload-model.json` - Unload a specific model
- `07-unload-active.json` - Unload the currently active model

## Endpoint Examples (Copy-Paste Ready)

### 1. GET /health
**Description**: Health check with hardware information

**No request body needed** - just click Execute in Swagger UI

**Example Response**:
```json
{
  "status": "ok",
  "gpu": {
    "name": "NVIDIA GeForce RTX 3090",
    "vramBytes": 25769803776,
    "cudaCapable": true
  },
  "cpu": {
    "name": "Intel Core i9-12900K",
    "cores": 16
  },
  "activeModel": "tinyllama"
}
```

---

### 2. GET /metrics
**Description**: Prometheus-compatible metrics

**No request body needed**

---

### 3. GET /models
**Description**: List all registered models

**No request body needed**

**Example Response**:
```json
{
  "models": [
    {
      "modelId": "tinyllama",
      "status": "available",
      "sizeBytes": 637000000,
      "onDisk": true,
      "active": false
    }
  ]
}
```

---

### 3a. GET /models/catalog
**Description**: Get catalog of available downloadable models from HuggingFace

**Query Parameters**:
- `refresh` (optional, boolean): Force refresh of catalog (default: false, uses cached catalog if available)

**No request body needed**

**Example**: `GET /models/catalog` or `GET /models/catalog?refresh=true`

**Example Response**:
```json
{
  "models": [
    {
      "modelId": "tensorblock-qwen2.5-coder-7b-instruct",
      "repository": "tensorblock/Qwen2.5-Coder-7B-Instruct-GGUF",
      "category": "Coding",
      "description": "Qwen 2.5 Coder 7B - Excellent for code generation and understanding",
      "availableFiles": 12,
      "recommendedQuantization": {
        "fileName": "Qwen2.5-Coder-7B-Instruct-Q4_K_M.gguf",
        "fileSizeBytes": 5020000000,
        "downloadUrl": "https://huggingface.co/tensorblock/Qwen2.5-Coder-7B-Instruct-GGUF/resolve/main/Qwen2.5-Coder-7B-Instruct-Q4_K_M.gguf",
        "quantization": "Q4_K_M"
      },
      "allQuantizations": ["Q2_K", "Q3_K_S", "Q3_K_M", "Q4_0", "Q4_K_M", "Q5_K_M", "Q6_K", "Q8_0"],
      "source": "hf://tensorblock/Qwen2.5-Coder-7B-Instruct-GGUF/Qwen2.5-Coder-7B-Instruct-Q4_K_M.gguf",
      "lastChecked": "2025-12-16T13:30:00Z"
    }
  ],
  "count": 20,
  "categories": [
    { "category": "Coding", "count": 15 },
    { "category": "General", "count": 5 }
  ],
  "lastUpdated": "2025-12-16T13:30:00Z"
}
```

**Note**: This endpoint tests model availability without downloading. It checks HuggingFace repositories and verifies files are downloadable. Catalog is cached for 1 hour. Use `?refresh=true` to force a fresh check.

---

### 4. POST /models/download
**Description**: Download a model from HuggingFace

**Request Body** (copy-paste ready):
```json
{
  "modelId": "tinyllama",
  "source": "hf://TinyLlama/TinyLlama-1.1B-Chat-v1.0",
  "checksum": null,
  "priority": null
}
```

**Response (200 OK - small model)**:
```json
{
  "model": {
    "modelId": "tinyllama",
    "status": "available"
  }
}
```

**Response (202 Accepted - large model)**:
```json
{
  "jobId": "28d6ad75-83d4-4955-a719-cec810b84ca0"
}
```

**Note**: For 202 responses, use `GET /jobs/{jobId}` to check progress.

---

### 5. GET /jobs/{id}
**Description**: Get download job status

**Path Parameter**: `id` - The job ID from POST /models/download response

**Example**: `GET /jobs/28d6ad75-83d4-4955-a719-cec810b84ca0`

**Example Response**:
```json
{
  "jobId": "28d6ad75-83d4-4955-a719-cec810b84ca0",
  "status": "running",
  "progress": 0.45,
  "error": null
}
```

**Status values**: `queued`, `running`, `succeeded`, `failed`

---

### 6. POST /models/load
**Description**: Load a model into memory

**Request Body** (copy-paste ready):
```json
{
  "modelId": "tinyllama",
  "nCtx": 2048,
  "batch": 512,
  "nGpuLayers": 0,
  "offload": null
}
```

**All parameters are optional** - hardware-aware defaults are used if omitted.

**Example Response**:
```json
{
  "modelId": "tinyllama",
  "loaded": true,
  "params": {
    "nCtx": 2048,
    "batch": 512,
    "nGpuLayers": 0
  }
}
```

---

### 7. POST /models/active
**Description**: Set the active model

**Request Body** (copy-paste ready):
```json
{
  "modelId": "tinyllama"
}
```

**Example Response**:
```json
{
  "modelId": "tinyllama",
  "active": true
}
```

---

### 8. POST /models/unload
**Description**: Unload a model from memory

**Request Body** (copy-paste ready - unload specific model):
```json
{
  "modelId": "tinyllama"
}
```

**Or** (unload active model):
```json
{}
```

**Example Response**:
```json
{
  "modelId": "tinyllama",
  "unloaded": true
}
```

---

### 9. POST /sessions/{id}/reset
**Description**: Reset a chat session

**Path Parameter**: `id` - The session ID (e.g., "session-123")

**No request body needed**

**Example**: `POST /sessions/session-123/reset`

**Example Response**:
```json
{
  "sessionId": "session-123",
  "reset": true
}
```

---

### 10. POST /chat
**Description**: Chat completion endpoint (supports SSE and NDJSON)

**Request Body** (copy-paste ready - simple prompt):
```json
{
  "modelId": "tinyllama",
  "sessionId": null,
  "messages": null,
  "prompt": "Hello, how are you?",
  "maxTokens": 100,
  "temperature": 0.7,
  "topP": 0.95,
  "stop": null,
  "nCtx": null
}
```

**Request Body** (copy-paste ready - with messages):
```json
{
  "modelId": "tinyllama",
  "sessionId": "session-123",
  "messages": [
    {
      "role": "user",
      "content": "What is the capital of France?"
    },
    {
      "role": "assistant",
      "content": "The capital of France is Paris."
    },
    {
      "role": "user",
      "content": "What is the population of Paris?"
    }
  ],
  "prompt": null,
  "maxTokens": 200,
  "temperature": 0.8,
  "topP": 0.9,
  "stop": ["\n\n"],
  "nCtx": 4096
}
```

**Note**: 
- Set `Accept: text/event-stream` header for SSE format
- Omit Accept header for NDJSON format
- `modelId` is optional if a model is active

---

### 11. POST /chat/stream
**Description**: Chat completion with SSE streaming (always SSE)

**Request Body** (copy-paste ready):
```json
{
  "modelId": "tinyllama",
  "messages": [
    {
      "role": "user",
      "content": "What is AI?"
    }
  ],
  "maxTokens": 200,
  "temperature": 0.8
}
```

**Response Format**: Server-Sent Events (SSE)
```
event: token
data: "Hello"

event: token
data: " there"

event: end
data: {"usage": {"promptTokens": 5, "completionTokens": 2}, "timings": {...}}
```

---

## Complete Workflow Example

1. **Check health**: `GET /health`
2. **Download model**: `POST /models/download` with `01-download-model.json`
3. **Check job status**: `GET /jobs/{jobId}` (if 202 response)
4. **Load model**: `POST /models/load` with `02-load-model.json`
5. **Set active**: `POST /models/active` with `03-set-active-model.json`
6. **Chat**: `POST /chat` with `04-chat-simple.json` or `05-chat-with-messages.json`
7. **Unload**: `POST /models/unload` with `06-unload-model.json`

---

## Authentication

All endpoints require JWT authentication:

1. Get token from console output when starting API
2. In Swagger UI: Click "Authorize" → Paste token → Click "Authorize"
3. In curl/Postman: Add header `Authorization: Bearer YOUR_TOKEN`

---

## Troubleshooting

- **401 Unauthorized**: Make sure you've authorized in Swagger UI or included the JWT token in headers
- **404 Not Found**: Model might not be downloaded or loaded yet
- **422 Unprocessable Entity**: Check error message for details (e.g., model not loaded, invalid parameters)
- **500 Internal Server Error**: Check API logs in `./logs/` directory
