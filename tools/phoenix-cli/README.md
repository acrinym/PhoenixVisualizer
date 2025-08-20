# PhoenixVisualizer CLI Tool

A keyboard-driven menu system for common PhoenixVisualizer development tasks. This tool provides a reliable way to execute dotnet commands without worrying about PowerShell path issues or script compatibility.

## Features

- **Keyboard-driven menu** - No need to remember commands
- **Repo-aware** - Automatically finds PhoenixVisualizer.sln
- **Cross-platform** - Works on Windows, Linux, and macOS
- **Smart PowerShell detection** - Automatically finds pwsh or powershell
- **Integrated utilities** - BASS download, PowerShell aliases, etc.

## Quick Start

```bash
# From PhoenixVisualizer root directory
dotnet run --project tools/phoenix-cli

# Or use the launcher scripts
.\phoenix          # Windows batch
.\phoenix.ps1      # PowerShell
```

## Menu Options

### Core .NET Operations
- **A** - `dotnet --info` - Show .NET SDK information
- **B** - List SDKs and Runtimes
- **C** - Restore solution packages
- **D** - Clean solution
- **E** - Build solution
- **F** - Run PhoenixVisualizer.App
- **G** - Run PhoenixVisualizer.Editor
- **H** - Run tests
- **I** - Publish App (Release, single-file)
- **J** - List projects in solution
- **K** - List package references for App project
- **L** - Restore dotnet tools

### Windows-Specific Features
- **M** - Download BASS native libraries
- **N** - Load PowerShell aliases (phoenix, phoenix-editor)
- **O** - Export codebase structure (for ChatGPT analysis)

### Configuration
- **1** - Toggle between Debug/Release
- **2** - Change Runtime Identifier (win-x64, linux-x64, osx-x64)
- **?** - Show command cheatsheet
- **Q** - Quit

## Why Use This Tool?

### Problems Solved
- ✅ **No PowerShell PATH issues** - Works regardless of PowerShell installation
- ✅ **No script compatibility problems** - Pure .NET console app
- ✅ **Cross-platform** - Same interface on Windows, Linux, macOS
- ✅ **Repo-aware** - Automatically finds your PhoenixVisualizer.sln
- ✅ **Integrated utilities** - BASS download, PowerShell aliases in one place

### Traditional Methods vs CLI Tool
| Task | Traditional | CLI Tool |
|------|-------------|----------|
| Run PhoenixVisualizer | `.\run.ps1` (may fail) | Press **F** |
| Download BASS | `.\libs_etc\download_bass.ps1` | Press **M** |
| Build solution | `dotnet build` | Press **E** |
| Load aliases | `.\libs_etc\run-phoenix.ps1` | Press **N** |
| Export for ChatGPT | `.\libs_etc\export_script_fixed.ps1` | Press **O** |

## Building and Running

### Build
```bash
dotnet build tools/phoenix-cli
```

### Run
```bash
dotnet run --project tools/phoenix-cli
```

### Publish (for distribution)
```bash
dotnet publish tools/phoenix-cli -c Release -r win-x64 --self-contained false
```

## Integration

The CLI tool automatically integrates with your PhoenixVisualizer repository:

- **Solution detection** - Finds `PhoenixVisualizer.sln` automatically
- **Project paths** - Uses correct paths for App and Editor projects
- **Script integration** - Calls scripts from `libs_etc/` folder
- **Configuration** - Remembers Debug/Release and RID settings

## Troubleshooting

### "PhoenixVisualizer.sln not found"
- Make sure you're running from the PhoenixVisualizer repository root
- The tool will search parent directories automatically

### PowerShell commands fail
- The tool automatically detects pwsh vs powershell
- Uses full paths to avoid PATH issues
- Provides clear error messages

### Build fails
- Ensure you have .NET 8.0 SDK installed
- Run `dotnet --info` to check your installation

## Extending

The CLI tool is designed to be easily extensible:

1. **Add new menu options** - Add new cases to the switch statement
2. **New commands** - Create new helper methods
3. **Cross-platform features** - Use `RuntimeInformation.IsOSPlatform()`
4. **Configuration** - Add new configurable options

## License

Part of the PhoenixVisualizer project - same license terms apply.
