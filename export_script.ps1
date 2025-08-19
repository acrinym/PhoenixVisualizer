$output = "# Phoenix Visualizer PROJECT STRUCTURE DUMP`n`n"
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

$output | Out-File -FilePath "ritualos_project_dump.txt" -Encoding UTF8
Write-Host "Project dump created: Phoenix_Visualizer_project_dump.txt with $($files.Count) files"
