# PhoenixVisualizer Launcher Script
# This script actually launches PhoenixVisualizer

Write-Host "🚀 Starting PhoenixVisualizer..." -ForegroundColor Green

# Change to the project root directory
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

# Launch PhoenixVisualizer
dotnet run --project PhoenixVisualizer.App
