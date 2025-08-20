# PhoenixVisualizer PowerShell Launcher
# Source this file to add convenient aliases

function Start-PhoenixVisualizer {
    Write-Host "🚀 Starting PhoenixVisualizer..." -ForegroundColor Green
    dotnet run --project PhoenixVisualizer.App
}

function Start-PhoenixEditor {
    Write-Host "✏️ Starting PhoenixVisualizer Editor..." -ForegroundColor Blue
    dotnet run --project PhoenixVisualizer.Editor
}

# Create aliases
Set-Alias -Name phoenix -Value Start-PhoenixVisualizer
Set-Alias -Name phoenix-editor -Value Start-PhoenixEditor

Write-Host "✅ PhoenixVisualizer aliases loaded!" -ForegroundColor Green
Write-Host "Use 'phoenix' to run the main app" -ForegroundColor Yellow
Write-Host "Use 'phoenix-editor' to run the editor" -ForegroundColor Yellow
Write-Host "Use 'Start-PhoenixVisualizer' for the full function name" -ForegroundColor Yellow
