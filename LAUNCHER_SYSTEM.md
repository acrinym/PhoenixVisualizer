# 🚀 Launcher System Guide

## Overview

PhoenixVisualizer includes a comprehensive launcher system that makes it easy to run the application without remembering complex command-line arguments. This guide explains why `dotnet run` needs flags and how our launcher system solves this problem.

## 🤔 Why Can't I Just Use `dotnet run`?

### The Problem

When you try to run `dotnet run` from the solution root directory, you get this error:

```bash
Couldn't find a project to run. Ensure a project exists in the current directory, 
or pass the path to the project using --project.
```

### Root Causes

1. **Multiple Projects**: Your solution contains 10+ projects:
   - `PhoenixVisualizer.App` (executable)
   - `PhoenixVisualizer.Core` (library)
   - `PhoenixVisualizer.Audio` (library)
   - `PhoenixVisualizer.Visuals` (library)
   - `PhoenixVisualizer.PluginHost` (library)
   - And many more...

2. **No Default Startup Project**: .NET doesn't automatically know which project should run
3. **Solution vs Project Context**: `dotnet run` expects to be in a project directory, not a solution directory
4. **Complex Architecture**: Library projects vs executable projects

### The Solution

We've created multiple launcher options that handle the complexity for you:

## 🎯 Launcher Options

### Option 1: Double-click Launcher (Easiest)

**File**: `run.bat`

**Usage**: Just double-click `run.bat` in the root directory!

**What it does**:
```batch
@echo off
echo.
echo 🚀 PhoenixVisualizer Launcher
echo ================================
echo.
echo Starting PhoenixVisualizer...
echo.
dotnet run --project PhoenixVisualizer.App
echo.
echo Application closed. Press any key to exit...
pause >nul
```

**Benefits**:
- ✅ No command line needed
- ✅ User-friendly interface
- ✅ Handles all the complexity
- ✅ Works on any Windows system

### Option 2: PowerShell Aliases (Most Convenient)

**File**: `run-phoenix.ps1`

**Usage**:
```powershell
# Load the aliases once
. .\run-phoenix.ps1

# Then use:
phoenix          # Run main app
phoenix-editor   # Run editor
```

**What it provides**:
- **`phoenix`** alias for main application
- **`phoenix-editor`** alias for editor
- **`Start-PhoenixVisualizer`** full function name
- **`Start-PhoenixEditor`** full function name

**Benefits**:
- ✅ One-word commands
- ✅ Persistent across PowerShell sessions
- ✅ Professional development workflow
- ✅ Easy to remember

### Option 3: Direct Commands (For CI/CD)

**From solution root**:
```bash
dotnet run --project PhoenixVisualizer.App
```

**From project directory**:
```bash
cd PhoenixVisualizer.App
dotnet run
```

**Benefits**:
- ✅ Scriptable for automation
- ✅ CI/CD pipeline friendly
- ✅ No additional files needed
- ✅ Standard .NET workflow

### Option 4: Build and Run Executable

**Build the solution**:
```bash
dotnet build PhoenixVisualizer.sln
```

**Run the executable**:
```bash
.\PhoenixVisualizer.App\bin\Debug\net8.0\PhoenixVisualizer.exe
```

**Benefits**:
- ✅ No compilation on each run
- ✅ Fastest startup time
- ✅ Distribution ready
- ✅ Debugging friendly

## 🔧 How the Launchers Work

### Batch File (`run.bat`)

The batch file is a simple Windows script that:

1. **Displays a friendly header** with emojis and formatting
2. **Runs the correct command** with all necessary flags
3. **Handles the pause** so you can see any error messages
4. **Works on any Windows system** without additional software

### PowerShell Script (`run-phoenix.ps1`)

The PowerShell script creates functions and aliases:

```powershell
function Start-PhoenixVisualizer {
    Write-Host "🚀 Starting PhoenixVisualizer..." -ForegroundColor Green
    dotnet run --project PhoenixVisualizer.App
}

# Create aliases
Set-Alias -Name phoenix -Value Start-PhoenixVisualizer
Set-Alias -Name phoenix-editor -Value Start-PhoenixEditor
```

**Key Features**:
- **Function-based**: Full PowerShell functions with error handling
- **Alias creation**: Short, memorable commands
- **Color coding**: Visual feedback in the terminal
- **Reusable**: Load once, use many times

## 📁 File Structure

```
PhoenixVisualizer/
├── run.bat                    # Windows batch launcher
├── run.ps1                    # PowerShell launcher  
├── run-phoenix.ps1           # PowerShell aliases
├── PhoenixVisualizer.sln      # Solution file
├── PhoenixVisualizer.App/     # Main executable project
├── PhoenixVisualizer.Core/    # Core library
├── PhoenixVisualizer.Audio/   # Audio processing
└── ...                        # Other projects
```

## 🎯 Recommended Workflow

### For Development
1. **Use `run.bat`** for quick testing
2. **Use `phoenix` alias** for regular development
3. **Use `phoenix-editor`** for editor work

### For CI/CD
1. **Use direct commands** in scripts
2. **Specify project explicitly** for clarity
3. **Build executables** for distribution

### For Distribution
1. **Build release version** with `dotnet build -c Release`
2. **Distribute executables** from `bin/Release/` directories
3. **Include launcher scripts** for user convenience

## 🔍 Troubleshooting

### Common Issues

#### Launcher Not Working
- **Check file permissions**: Ensure launcher files are executable
- **Verify .NET installation**: Run `dotnet --version` to check
- **Check working directory**: Make sure you're in the solution root

#### PowerShell Execution Policy
If PowerShell scripts won't run:

```powershell
# Check current policy
Get-ExecutionPolicy

# Set to allow local scripts (if needed)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

#### Build Errors
If the launcher shows build errors:

1. **Restore packages**: `dotnet restore`
2. **Clean build**: `dotnet clean && dotnet build`
3. **Check dependencies**: Ensure all projects build individually

### Debug Information

#### Enable Verbose Output
```bash
dotnet run --project PhoenixVisualizer.App --verbosity detailed
```

#### Check Project Status
```bash
dotnet sln list
dotnet project list
```

#### Verify Dependencies
```bash
dotnet restore --verbosity detailed
```

## 🚀 Advanced Usage

### Custom Launcher Scripts

You can create custom launcher scripts for specific scenarios:

**Development with debugging**:
```batch
@echo off
echo Starting PhoenixVisualizer in Debug Mode...
dotnet run --project PhoenixVisualizer.App --configuration Debug
pause
```

**Release testing**:
```batch
@echo off
echo Starting PhoenixVisualizer in Release Mode...
dotnet run --project PhoenixVisualizer.App --configuration Release
pause
```

### PowerShell Profile Integration

Add the aliases to your PowerShell profile for permanent access:

1. **Edit profile**: `notepad $PROFILE`
2. **Add line**: `. "D:\GitHub\AMrepo\PhoenixVisualizer\run-phoenix.ps1"`
3. **Save and restart**: PowerShell will load aliases automatically

### Environment-Specific Launchers

Create launchers for different environments:

**`run-dev.bat`**:
```batch
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --project PhoenixVisualizer.App
```

**`run-prod.bat`**:
```batch
set ASPNETCORE_ENVIRONMENT=Production
dotnet run --project PhoenixVisualizer.App --configuration Release
```

## 🔮 Future Enhancements

### Planned Improvements
- **Cross-platform launchers**: Bash scripts for Linux/macOS
- **Configuration integration**: Launcher settings in app config
- **Plugin launchers**: Direct plugin testing from launchers
- **Performance profiling**: Built-in performance monitoring

### Integration Opportunities
- **IDE integration**: Visual Studio and VS Code launchers
- **Docker support**: Container-based launchers
- **Cloud deployment**: Azure/AWS deployment launchers
- **CI/CD integration**: Automated testing launchers

## 📚 Related Documentation

- [RUNNING.md](RUNNING.md) - Complete running guide
- [PLUGIN_MANAGEMENT.md](PLUGIN_MANAGEMENT.md) - Plugin system guide
- [WINAMP_PLUGIN_SETUP.md](WINAMP_PLUGIN_SETUP.md) - Winamp integration
- [TODO.md](TODO.md) - Development roadmap

---

## 🎉 Summary

The launcher system solves the `dotnet run` complexity by providing:

1. **`run.bat`** - Double-click to run (easiest)
2. **`phoenix` alias** - One word to run (most convenient)
3. **Direct commands** - Full control (for automation)
4. **Executable builds** - Fastest startup (for distribution)

**No more remembering `--project` flags!** 🎊
