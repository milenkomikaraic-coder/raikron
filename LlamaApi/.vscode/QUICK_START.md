# Quick Start - Debugging in Cursor

## How to Start the API

1. **Open Run and Debug Panel**: Press `Ctrl+Shift+D` (or click the debug icon in sidebar)

2. **Select Configuration**: 
   - Choose **".NET Core Launch (API)"** from the dropdown at the top
   - This is the configuration in `LlamaApi/.vscode/launch.json`

3. **Start Debugging**: Press `F5` (or click the green play button)

4. **What Happens**:
   - Builds the project automatically
   - Starts the API
   - Opens browser at `http://localhost:5000` (Swagger UI)
   - Shows console output in integrated terminal

## Alternative: Using Root Workspace Configuration

If you're in the root workspace (RAIKRON folder):

1. Select **".NET Core Launch (Llama API)"** from the dropdown
2. Press `F5`

## Troubleshooting

**If it says "preLaunchTask 'build' failed":**
- Press `Ctrl+Shift+P` → "Tasks: Run Task" → "restore"
- Then try again

**If DLL not found:**
- Make sure you built the project: `Ctrl+Shift+B`
- Check that `LlamaApi/bin/Debug/net9.0/LlamaApi.dll` exists

**If port 5000 is in use:**
- Stop any running instances
- Or change port in `Properties/launchSettings.json`

## Keyboard Shortcuts

- `F5` - Start debugging
- `Ctrl+F5` - Run without debugging  
- `Shift+F5` - Stop debugging
- `F9` - Toggle breakpoint
- `Ctrl+Shift+B` - Build project
