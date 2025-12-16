# API Test Requests

This directory contains JSON request bodies for testing all API endpoints.

## Usage

1. Copy the JSON content from any file below
2. Open Swagger UI at `http://localhost:5000`
3. Navigate to the corresponding endpoint
4. Click "Try it out"
5. Paste the JSON into the request body
6. Click "Execute"

## Files

- **01-download-model.json** - Download TinyLlama (original repo - may not have GGUF files)
- **01-download-model-gguf.json** - Download TinyLlama from TheBloke's GGUF repository (RECOMMENDED)
- **02-load-model.json** - Load a model into memory
- **03-set-active-model.json** - Set a model as active
- **04-chat-simple.json** - Simple chat with a prompt
- **05-chat-with-messages.json** - Chat with conversation history
- **06-unload-model.json** - Unload a specific model
- **07-unload-active.json** - Unload the currently active model

## Important Note About Model Downloads

**Default Model: Qwen Coder 7B**

The default model is configured to use:
- Repository: `tensorblock/Qwen2.5-Coder-7B-Instruct-GGUF`
- File: `Qwen2.5-Coder-7B-Instruct-Q4_K_M.gguf` (Q4_K_M quantization - good balance)

**Alternative repositories for Qwen Coder 7B:**
- `hf://tensorblock/Qwen2.5-Coder-7B-Instruct-GGUF` (recommended - multiple quantizations)
- `hf://prithivMLmods/Qwen2.5-Coder-7B-Instruct-GGUF`

**For TinyLlama models:**
- `hf://TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF` (recommended)
- `hf://tensorblock/TinyLlama-1.1B-Chat-v1.0-GGUF`

## Authentication

All requests require a JWT token. Get the dev token from the console output when starting the API, then:
- In Swagger UI: Click "Authorize" button and paste the token
- In curl/Postman: Add header `Authorization: Bearer <token>`

## Example Workflow

1. **Download model**: Use `01-download-model-gguf.json` (TheBloke's GGUF repo)
   - Check job status: `GET /jobs/{jobId}`

2. **Load model**: Use `02-load-model.json`

3. **Set active**: Use `03-set-active-model.json`

4. **Chat**: Use `04-chat-simple.json` or `05-chat-with-messages.json`

5. **Unload**: Use `06-unload-model.json` or `07-unload-active.json`
