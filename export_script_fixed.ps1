$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Config
$outputFile = Join-Path (Get-Location) 'ritualos_project_dump.txt'

# Header (here-string avoids backtick/quote escape gotchas)
$output = @"
# Phoenix Visualizer PROJECT STRUCTURE DUMP

"@

# Collect files
$files = Get-ChildItem -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Extension -match '\.(cs|axaml|csproj|json|md|txt|sh|ps1|py|html)$' -and
        $_.FullName -notmatch '\\(libs|bin|obj|tools|\.git|\.vscode)\\' -and
        $_.FullName -notmatch 'README_SPRINT.*\.md|RitualOS_TODO\.md|wishlist\.md'
    } |
    Sort-Object FullName

# Exclude any prior dump output if present
$files = $files | Where-Object { $_.FullName -ne $outputFile }

foreach ($file in $files) {
    $relativePath = try { Resolve-Path -Relative $file.FullName } catch { $file.FullName }
    $output += "[$relativePath]`r`n"
    try {
        $content = Get-Content $file.FullName -Raw -ErrorAction Stop
        $output += $content + "`r`n`r`n"
    } catch {
        $output += "ERROR READING FILE: $($_.Exception.Message)`r`n`r`n"
    }
}

$output | Out-File -FilePath $outputFile -Encoding UTF8
Write-Host "Project dump created: $outputFile with $($files.Count) files"
