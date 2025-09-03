# Phoenix Visualizer Export Script with Build Status Tracking
# This script creates a comprehensive text export of the entire codebase
# INCLUDING BUILD STATUS AND ERROR TRACKING
# 
# Usage: Run this script from the PhoenixVisualizer project root directory
# Requirements: .NET SDK must be installed and accessible

param(
    [string]$OutputPath = ".",
    [switch]$Verbose
)

# Generate filename with current date
$date = Get-Date -Format "yyyy-MM-dd"
$filename = Join-Path $OutputPath "phoenix_visualizer_source_export_$date.txt"

Write-Host "Starting Phoenix Visualizer export with build status tracking..." -ForegroundColor Green
Write-Host "Output file: $filename" -ForegroundColor Yellow

$output = @"
================================================================================
PHOENIX VISUALIZER - SOURCE CODE EXPORT (STREAMLINED)
================================================================================
Generated: $(Get-Date)
Description: Streamlined source code export containing ONLY essential source files.
             Excludes: binaries, obj/bin directories, generated files, documentation,
             large files, and compiled assemblies.

             This export contains ONLY:
             - C# source files (.cs)
             - XAML files (.axaml)
             - Project files (.csproj, .sln)
             - Small configuration files
             - Markdown build info and info on current state, documentation, etc.

             Generated automatically on: $date

================================================================================
BUILD STATUS & ERROR TRACKING
================================================================================
Build started at: $(Get-Date)

"@

# Run dotnet build and capture output
$output += "=== DOTNET BUILD OUTPUT ===`n"
$output += "Build Command: dotnet build PhoenixVisualizer.sln`n"
$output += "Build Timestamp: $(Get-Date)`n`n"

try {
    Write-Host "Running dotnet build to capture current build status..." -ForegroundColor Cyan
    
    # Capture build output
    $buildOutput = & dotnet build PhoenixVisualizer.sln 2>&1
    $buildExitCode = $LASTEXITCODE
    
    $output += $buildOutput -join "`n"
    $output += "`n`n"
    
    # Analyze build results
    $errorCount = ($buildOutput | Select-String "error CS" | Measure-Object).Count
    $warningCount = ($buildOutput | Select-String "warning CS" | Measure-Object).Count
    
    $output += "=== BUILD SUMMARY ===`n"
    $output += "Build Exit Code: $buildExitCode`n"
    $output += "Build Status: $(if ($buildExitCode -eq 0) { 'SUCCESS' } else { 'FAILED' })`n"
    $output += "Build Completed: $(Get-Date)`n"
    $output += "Total Errors: $errorCount`n"
    $output += "Total Warnings: $warningCount`n`n"
    
    if ($buildExitCode -ne 0) {
        $output += "=== BUILD FAILURE DETAILS ===`n"
        $output += "The build failed with $errorCount errors and $warningCount warnings.`n"
        $output += "This export captures the broken state for debugging purposes.`n`n"
        
        # Extract and list specific errors
        $output += "=== ERROR BREAKDOWN ===`n"
        $errors = $buildOutput | Select-String "error CS" | Select-Object -First 20
        $output += $errors -join "`n"
        if ($errorCount -gt 20) {
            $output += "`n... and $($errorCount - 20) more errors`n"
        }
        $output += "`n"
        
        Write-Host "❌ Build FAILED with $errorCount errors and $warningCount warnings" -ForegroundColor Red
    } else {
        $output += "=== BUILD SUCCESS ===`n"
        $output += "✅ Build completed successfully with $warningCount warnings`n"
        $output += "This export captures a working build state.`n`n"
        
        Write-Host "✅ Build SUCCESS with $warningCount warnings" -ForegroundColor Green
    }
    
    $output += "=================================================================================`n"
    $output += "END OF BUILD STATUS`n"
    $output += "=================================================================================`n`n"
    
} catch {
    $output += "ERROR: Failed to run dotnet build: $($_.Exception.Message)`n`n"
    Write-Host "❌ Failed to run dotnet build: $($_.Exception.Message)" -ForegroundColor Red
}

$output += "================================================================================`n"
$output += "SOURCE CODE EXPORT`n"
$output += "================================================================================`n`n"

Write-Host "Collecting source files..." -ForegroundColor Cyan

# Get only essential source files (EXCLUDE binaries and bloated files)
$files = Get-ChildItem -Recurse -File | Where-Object {
    # Include only these file extensions
    $_.Extension -match "\.(cs|axaml|csproj|sln|md)$" -and
    # Exclude problematic directories and files
    $_.FullName -notmatch "\\bin\\|\\obj\\|\\\.git\\|\\\.dotnet\\|\\\.vscode\\|libs_etc\\|libs\\|misc\\|tools\\|DreamDictionary\\" -and
    $_.Name -notmatch "^\..*" -and  # Exclude hidden files
    $_.Name -notmatch "AssemblyInfo\.cs$" -and  # Exclude assembly info files
    $_.Name -notmatch ".*\.g\.cs$" -and  # Exclude generated files
    $_.Name -notmatch ".*\.generated\.cs$" -and  # Exclude generated files
    $_.Length -lt 500000  # Exclude files larger than 500KB (binaries, etc.)
} | Where-Object {
    # Additional filtering for content-based exclusion
    $content = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
    if ($content) {
        # Exclude files that contain assembly attributes (compiled DLL metadata)
        -not ($content -match "\[assembly:") -and
        # Exclude files that are mostly binary or contain weird characters
        -not ($content -match "[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F]") -and
        # Exclude extremely large files
        ($content.Length -lt 100000)
    } else {
        $true
    }
} | Sort-Object FullName

$fileCount = $files.Count
Write-Host "Found $fileCount source files to export..." -ForegroundColor Cyan

$processedCount = 0
foreach ($file in $files) {
    $processedCount++
    if ($Verbose -or ($processedCount % 50 -eq 0)) {
        Write-Progress -Activity "Exporting source files" -Status "Processing $($file.Name)" -PercentComplete (($processedCount / $fileCount) * 100)
        Write-Host "Processing $processedCount/$fileCount : $($file.Name)" -ForegroundColor Gray
    }
    
    $relativePath = $file.FullName.Replace((Get-Location).Path + "\", "")
    $output += "[$relativePath]`n"
    try {
        $content = Get-Content $file.FullName -Raw -ErrorAction Stop
        $output += $content + "`n`n"
    } catch {
        $output += "ERROR READING FILE: $($_.Exception.Message)`n`n"
        Write-Host "⚠️  Error reading $($file.Name): $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# Save to file
Write-Host "Saving export to $filename..." -ForegroundColor Cyan
$output | Out-File -FilePath $filename -Encoding UTF8

# Get file size
$fileSize = (Get-Item $filename).Length
$fileSizeMB = [math]::Round($fileSize / 1MB, 2)

Write-Host "`n🎉 Export completed successfully!" -ForegroundColor Green
Write-Host "File: $filename" -ForegroundColor White
Write-Host "Size: $fileSizeMB MB" -ForegroundColor White
Write-Host "Files processed: $processedCount" -ForegroundColor White
Write-Host "Build Status: $(if ($buildExitCode -eq 0) { 'SUCCESS' } else { 'FAILED' }) with $errorCount errors and $warningCount warnings" -ForegroundColor $(if ($buildExitCode -eq 0) { 'Green' } else { 'Red' })
Write-Host "`nThis export serves as a backup/restore point for the Phoenix Visualizer project state." -ForegroundColor Cyan
Write-Host "Use this file to track project progress and restore working states if needed." -ForegroundColor Cyan