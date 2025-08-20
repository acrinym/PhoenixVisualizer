# PhoenixVisualizer Utility Scripts

This folder contains utility scripts and tools for PhoenixVisualizer development and deployment.

## Scripts

### Export Scripts
- **`export_script.ps1`** - Basic project structure export script
- **`export_script_fixed.ps1`** - Enhanced project structure export script with better error handling

### Launcher Scripts
- **`run-phoenix.ps1`** - PhoenixVisualizer function library (aliases and functions)
- **`launch-phoenix.ps1`** - Working PhoenixVisualizer launcher script

### Root Directory Launchers
- **`run.ps1`** - PowerShell launcher (direct dotnet call)
- **`run.bat`** - Batch launcher (direct dotnet call)
- **`run-direct.cmd`** - Direct CMD launcher (no PowerShell dependency)

### CLI Tool (Recommended!)
- **`phoenix`** - Batch launcher for PhoenixVisualizer CLI menu
- **`phoenix.ps1`** - PowerShell launcher for PhoenixVisualizer CLI menu
- **`tools/phoenix-cli/`** - CLI tool source code and project

### Library Scripts
- **`download_bass.ps1`** - Downloads and installs BASS audio library

## Usage

### From Root Directory
```powershell
# PhoenixVisualizer CLI Menu (Recommended!)
.\phoenix
# or
.\phoenix.ps1

# Traditional launchers
.\run.ps1
.\run.bat
.\run-direct.cmd
```

### From libs_etc Directory
```powershell
# Export project structure
.\export_script_fixed.ps1

# Run PhoenixVisualizer (working launcher)
.\launch-phoenix.ps1

# Load PhoenixVisualizer functions and aliases
.\run-phoenix.ps1

# Download BASS library
.\download_bass.ps1
```

## Organization

All utility scripts are kept in this folder to maintain a clean root directory structure. The root directory contains only:
- Project files (.sln, .csproj)
- Documentation (.md files)
- Source code directories
- Configuration files

## Notes

- Scripts in the root directory are simple launchers that call the actual scripts in `libs_etc`
- This keeps the root clean while maintaining easy access to utilities
- All scripts use relative paths to work from any location
