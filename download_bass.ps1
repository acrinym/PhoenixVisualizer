# Download BASS Native Libraries for PhoenixVisualizer
# This script downloads the required native DLLs that ManagedBass depends on

Write-Host "Downloading BASS Native Libraries..." -ForegroundColor Green

$libsDir = "libs"
if (!(Test-Path $libsDir)) {
    New-Item -ItemType Directory -Path $libsDir | Out-Null
}

# BASS Core Library (x64)
$bassUrl = "https://www.un4seen.com/files/bass24.zip"
$bassZip = "$libsDir\bass24.zip"
$bassExtract = "$libsDir\bass24"

Write-Host "Downloading BASS Core..." -ForegroundColor Yellow
Invoke-WebRequest -Uri $bassUrl -OutFile $bassZip

Write-Host "Extracting BASS Core..." -ForegroundColor Yellow
Expand-Archive -Path $bassZip -DestinationPath $bassExtract -Force

# Copy the x64 DLL to the main libs directory
$bassDll = "$bassExtract\x64\bass.dll"
if (Test-Path $bassDll) {
    Copy-Item $bassDll -Destination "$libsDir\bass.dll" -Force
    Write-Host "✓ BASS Core DLL copied" -ForegroundColor Green
} else {
    Write-Host "✗ BASS Core DLL not found" -ForegroundColor Red
}

# BASS FX Library (x64)
$bassFxUrl = "https://www.un4seen.com/files/bass_fx24.zip"
$bassFxZip = "$libsDir\bass_fx24.zip"
$bassFxExtract = "$libsDir\bass_fx24"

Write-Host "Downloading BASS FX..." -ForegroundColor Yellow
Invoke-WebRequest -Uri $bassFxUrl -OutFile $bassFxZip

Write-Host "Extracting BASS FX..." -ForegroundColor Yellow
Expand-Archive -Path $bassFxZip -DestinationPath $bassFxExtract -Force

# Copy the x64 DLL to the main libs directory
$bassFxDll = "$bassFxExtract\x64\bass_fx.dll"
if (Test-Path $bassFxDll) {
    Copy-Item $bassFxDll -Destination "$libsDir\bass_fx.dll" -Force
    Write-Host "✓ BASS FX DLL copied" -ForegroundColor Green
} else {
    Write-Host "✗ BASS FX DLL not found" -ForegroundColor Red
}

# Clean up temporary files
Write-Host "Cleaning up..." -ForegroundColor Yellow
Remove-Item $bassZip -Force -ErrorAction SilentlyContinue
Remove-Item $bassFxZip -Force -ErrorAction SilentlyContinue
Remove-Item $bassExtract -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $bassFxExtract -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`nBASS Libraries downloaded to: $libsDir" -ForegroundColor Green
Write-Host "Files:" -ForegroundColor Cyan
Get-ChildItem $libsDir -Name

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Copy these DLLs to your output directory" -ForegroundColor White
Write-Host "2. Or add them to your project as content files" -ForegroundColor White
Write-Host "3. Build and run PhoenixVisualizer" -ForegroundColor White
