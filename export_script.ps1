# Phoenix Visualizer Export Script with Build Status Tracking
# This script creates a comprehensive text export of the entire codebase
# INCLUDING BUILD STATUS AND ERROR TRACKING

$output = "# Phoenix Visualizer PROJECT STRUCTURE DUMP WITH BUILD STATUS`n`n"

# Generate filename with current date
$date = Get-Date -Format "yyyy-MM-dd"
$filename = "phoenix_visualizer_source_export_$date.txt"

# Add build status section
$output += "================================================================================`n"
$output += "BUILD STATUS & ERROR TRACKING`n"
$output += "================================================================================`n"
$output += "Build started at: $(Get-Date)`n`n"

# Run dotnet build and capture output
$output += "=== DOTNET BUILD OUTPUT ===`n"
$output += "Build Command: dotnet build PhoenixVisualizer.sln`n"
$output += "Build Timestamp: $(Get-Date)`n`n"

try {
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
    } else {
        $output += "=== BUILD SUCCESS ===`n"
        $output += "✅ Build completed successfully with $warningCount warnings`n"
        $output += "This export captures a working build state.`n`n"
    }
    
    $output += "=================================================================================`n"
    $output += "END OF BUILD STATUS`n"
    $output += "=================================================================================`n`n"
    
} catch {
    $output += "ERROR: Failed to run dotnet build: $($_.Exception.Message)`n`n"
}

$output += "================================================================================`n"
$output += "SOURCE CODE EXPORT`n"
$output += "================================================================================`n`n"

# Get all source files
$files = Get-ChildItem -Recurse -File | Where-Object { 
    $_.Extension -match "\.(cs|axaml|csproj|json|md|txt|sh|ps1|py|html)$" -and 
    $_.FullName -notmatch "\\libs\\|\\bin\\|\\obj\\|\\tools\\|\\DreamDictionary\\|\.git\\|\.dotnet\\|\.vscode\\|misc\\|allfiles\.txt|update_.*\.ps1|fix_.*\.ps1|simplify_.*\.ps1|combine_.*\.py|convert_.*\.py|process_.*\.py|capture\.sh|PHASE2_FEATURES\.md|PORTABLE_THEMEBUILDER\.md|project_context\.txt|README_SPRINT.*\.md|RitualOS_TODO\.md|wishlist\.md" 
} | Sort-Object FullName

foreach ($file in $files) {
    $relativePath = $file.FullName.Replace((Get-Location).Path + "\", "")
    $output += "[$relativePath]`n"
    try {
        $content = Get-Content $file.FullName -Raw -ErrorAction Stop
        $output += $content + "`n`n"
    } catch {
        $output += "ERROR READING FILE: $($_.Exception.Message)`n`n"
    }
}

# Save to file
$output | Out-File -FilePath $filename -Encoding UTF8

Write-Host "Project dump created: $filename with $($files.Count) files"
Write-Host "This export includes build status and serves as a backup/restore point for the Phoenix Visualizer project state."
Write-Host "Build Status: $(if ($buildExitCode -eq 0) { 'SUCCESS' } else { 'FAILED' }) with $errorCount errors and $warningCount warnings"
