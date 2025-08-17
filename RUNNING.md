# 🚀 Running PhoenixVisualizer

## Quick Start Options

### Option 1: Double-click the batch file (Windows)
```
run.bat
```
Just double-click `run.bat` in the root directory!

### Option 2: PowerShell with aliases
```powershell
# Load the aliases
. .\run-phoenix.ps1

# Then use:
phoenix          # Run main app
phoenix-editor   # Run editor
```

### Option 3: Direct dotnet commands
```bash
# From solution root:
dotnet run --project PhoenixVisualizer.App

# From project directory:
cd PhoenixVisualizer.App
dotnet run
```

### Option 4: Build and run executable
```bash
# Build the solution
dotnet build PhoenixVisualizer.sln

# Run the executable
.\PhoenixVisualizer.App\bin\Debug\net8.0\PhoenixVisualizer.exe
```

## Why can't I just use `dotnet run`?

The reason you can't use just `dotnet run` from the solution root is because:

1. **Multiple projects**: The solution contains multiple projects (App, Core, Audio, Visuals, etc.)
2. **No default startup project**: .NET doesn't automatically know which project should run
3. **Solution vs Project**: `dotnet run` expects to be run from a project directory, not a solution directory

## Recommended Workflow

1. **For development**: Use `run.bat` or the PowerShell aliases
2. **For CI/CD**: Use `dotnet run --project PhoenixVisualizer.App`
3. **For distribution**: Build and distribute the executable

## Troubleshooting

- **Build errors**: Make sure you're in the right directory
- **Missing dependencies**: Run `dotnet restore` first
- **Permission issues**: Run PowerShell as Administrator if needed

## File Structure

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
