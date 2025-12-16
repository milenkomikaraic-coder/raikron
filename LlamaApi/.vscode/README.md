# Cursor/VS Code Configuration

This directory contains Cursor (and VS Code compatible) configuration files for the Llama API project.

**Note**: Cursor is fully compatible with VS Code configurations. All settings work in both editors.

## Features

- **Debugging**: Launch configurations for debugging the API
- **Build Tasks**: Clean, build, restore, watch, and run tasks
- **Code Formatting**: Auto-format on save with C# best practices
- **IntelliSense**: Full C# language support with OmniSharp

## Quick Start

1. **Open in Cursor**: Open the `LlamaApi` folder in Cursor

2. **Install Recommended Extensions**:
   - Cursor will prompt you to install recommended extensions
   - Or install manually: `Ctrl+Shift+P` → "Extensions: Show Recommended Extensions"
   - Essential: C# Dev Kit, C# extension

3. **Debug the API**:
   - Press `F5` or go to Run and Debug (`Ctrl+Shift+D`)
   - Select ".NET Core Launch (API)"
   - Set breakpoints and debug!
   - The API will auto-open in your browser at `http://localhost:5000`

4. **Build Tasks**:
   - Press `Ctrl+Shift+B` to build (default task)
   - Or `Ctrl+Shift+P` → "Tasks: Run Task"
   - Available tasks:
     - **build** (default) - Build the project (`Ctrl+Shift+B`)
     - **clean** - Clean build artifacts
     - **restore** - Restore NuGet packages
     - **clean and rebuild** - Clean then build
     - **watch** - Watch for changes and rebuild
     - **run** - Run the API

## Keyboard Shortcuts

- `F5` - Start debugging
- `Ctrl+F5` - Run without debugging
- `Shift+F5` - Stop debugging
- `F9` - Toggle breakpoint
- `F10` - Step over
- `F11` - Step into
- `Shift+F11` - Step out

## Configuration Files

- **launch.json** - Debug configurations
- **tasks.json** - Build and run tasks
- **settings.json** - Editor and C# settings
- **extensions.json** - Recommended VS Code extensions

## Cursor-Specific Features

- **AI Chat**: Use Cursor's built-in AI chat for code assistance
- **Composer**: Use Cursor Composer for multi-file edits
- **Auto-save**: Enabled by default for better AI integration
- **All VS Code features**: Full compatibility with VS Code extensions and settings

## Troubleshooting

If IntelliSense isn't working:
1. Ensure C# Dev Kit extension is installed
2. Run: `Ctrl+Shift+P` → "OmniSharp: Restart OmniSharp"
3. Check Output panel → "OmniSharp Log"
4. Try: `Ctrl+Shift+P` → "Developer: Reload Window"

If build fails:
1. Run: `Ctrl+Shift+P` → "Tasks: Run Task" → "restore"
2. Then run: "build" or press `Ctrl+Shift+B`

If debugging doesn't start:
1. Ensure the project builds successfully first
2. Check that port 5000 is not in use
3. Verify `launchSettings.json` exists in Properties folder
