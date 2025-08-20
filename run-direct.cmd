@echo off
REM Direct launcher - calls dotnet directly without PowerShell dependency
echo Starting PhoenixVisualizer...
cd /d "%~dp0"
dotnet run --project PhoenixVisualizer.App
pause
