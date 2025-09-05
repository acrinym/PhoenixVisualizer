# Phoenix Visualizer Codebase Export Script
# Builds the project, captures diagnostics, and exports all source files

param(
    [string]$OutputFile = "outsource.txt",
    [switch]$SkipBuild = $false
)

Write-Host "=== Phoenix Visualizer Codebase Export ===" -ForegroundColor Green
Write-Host "Output file: $OutputFile" -ForegroundColor Yellow

# Clear previous output file
if (Test-Path $OutputFile) {
    Remove-Item $OutputFile -Force
    Write-Host "Removed existing $OutputFile" -ForegroundColor Yellow
}

# Build the project and capture diagnostics
if (-not $SkipBuild) {
    Write-Host "`n=== Building Project ===" -ForegroundColor Cyan
    
    # Build all projects
    $buildOutput = @()
    $buildOutput += "=== BUILD OUTPUT ==="
    $buildOutput += "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    $buildOutput += ""
    
    # Build main solution
    Write-Host "Building main solution..." -ForegroundColor White
    $buildResult = dotnet build 2>&1
    $buildOutput += "dotnet build:"
    $buildOutput += $buildResult
    $buildOutput += ""
    
    # Build individual projects if needed
    $projects = @(
        "PhoenixVisualizer.App",
        "PhoenixVisualizer.Editor", 
        "PhoenixVisualizer.Rendering",
        "PhoenixVisualizer.Visuals",
        "PhoenixVisualizer.Audio",
        "PhoenixVisualizer.Core",
        "PhoenixVisualizer.Plugins.Avs",
        "PhoenixVisualizer.Plugins.Ape.Phoenix"
    )
    
    foreach ($project in $projects) {
        if (Test-Path "$project\$project.csproj") {
            Write-Host "Building $project..." -ForegroundColor White
            $projectResult = dotnet build $project 2>&1
            $buildOutput += "dotnet build ${project}:"
            $buildOutput += $projectResult
            $buildOutput += ""
        }
    }
    
    # Write build output to file
    $buildOutput | Out-File -FilePath $OutputFile -Encoding UTF8
    Write-Host "Build diagnostics written to $OutputFile" -ForegroundColor Green
}

# Get all source files
Write-Host "`n=== Collecting Source Files ===" -ForegroundColor Cyan

$sourceFiles = Get-ChildItem -Recurse -File | Where-Object { 
    $_.Extension -match "\.(cs|csproj|axaml|md|txt|json|xml)$" -and 
    $_.FullName -notmatch "source_backup|backup|bin|obj|\.git|outsource|dirstrct|ritualos_project_dump" -and
    $_.Name -ne "outsource.txt" -and
    $_.Name -ne "export_codebase.ps1"
}

Write-Host "Found $($sourceFiles.Count) source files" -ForegroundColor White

# Add file count to output
Add-Content -Path $OutputFile -Value ""
Add-Content -Path $OutputFile -Value "=== SOURCE FILES EXPORT ==="
Add-Content -Path $OutputFile -Value "Total files: $($sourceFiles.Count)"
Add-Content -Path $OutputFile -Value "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Add-Content -Path $OutputFile -Value ""

# Export each file with directory structure
foreach ($file in $sourceFiles) {
    $relativePath = $file.FullName.Replace((Get-Location).Path, "").TrimStart("\")
    $relativePath = $relativePath.Replace("\", "/")  # Use forward slashes for consistency
    
    Write-Host "Exporting: $relativePath" -ForegroundColor Gray
    
    Add-Content -Path $OutputFile -Value ""
    Add-Content -Path $OutputFile -Value "=== $relativePath ==="
    Add-Content -Path $OutputFile -Value ""
    
    # Read file content and add it
    try {
        $content = Get-Content $file.FullName -Raw -Encoding UTF8
        if ($content) {
            Add-Content -Path $OutputFile -Value $content
        } else {
            Add-Content -Path $OutputFile -Value "# Empty file"
        }
    }
    catch {
        Add-Content -Path $OutputFile -Value "# Error reading file: $($_.Exception.Message)"
    }
}

# Add summary
$finalSize = (Get-ChildItem $OutputFile).Length
$finalSizeMB = [math]::Round($finalSize / 1MB, 2)
$finalSizeKB = [math]::Round($finalSize / 1KB, 2)

Add-Content -Path $OutputFile -Value ""
Add-Content -Path $OutputFile -Value "=== EXPORT SUMMARY ==="
Add-Content -Path $OutputFile -Value "Total files exported: $($sourceFiles.Count)"
Add-Content -Path $OutputFile -Value "Export file size: $finalSizeMB MB ($finalSizeKB KB)"
Add-Content -Path $OutputFile -Value "Export completed: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

Write-Host "`n=== Export Complete ===" -ForegroundColor Green
Write-Host "Files exported: $($sourceFiles.Count)" -ForegroundColor White
Write-Host "Output file: $OutputFile" -ForegroundColor White
Write-Host "File size: $finalSizeMB MB ($finalSizeKB KB)" -ForegroundColor White
Write-Host "`nExport saved to: $(Resolve-Path $OutputFile)" -ForegroundColor Yellow
