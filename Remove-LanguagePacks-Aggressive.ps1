# Remove-LanguagePacks-Aggressive.ps1
# Aggressive script to remove non-English language packs and features
# Run as Administrator to free up significant disk space

param(
    [switch]$WhatIf = $false,
    [switch]$Force = $false,
    [string[]]$KeepLanguages = @("en-US", "en-GB", "en")
)

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "This script requires Administrator privileges!" -ForegroundColor Red
    Write-Host "Please run PowerShell as Administrator and try again." -ForegroundColor Yellow
    exit 1
}

Write-Host "=== AGGRESSIVE Language Pack Cleanup Script ===" -ForegroundColor Cyan
Write-Host "This script will remove non-English language packs from:" -ForegroundColor Yellow
Write-Host "- Windows Store Apps (WindowsApps)" -ForegroundColor Yellow
Write-Host "- Metro Apps (Program Files)" -ForegroundColor Yellow
Write-Host "- Windows Language Features" -ForegroundColor Yellow
Write-Host "- Additional Language Resources" -ForegroundColor Yellow
Write-Host "Keeping languages: $($KeepLanguages -join ', ')" -ForegroundColor Green

if ($WhatIf) {
    Write-Host "`n[WHAT-IF MODE] - No files will be deleted" -ForegroundColor Magenta
}

if (-not $Force) {
    $confirmation = Read-Host "`nWARNING: This is AGGRESSIVE mode. Do you want to continue? (y/N)"
    if ($confirmation -ne "y" -and $confirmation -ne "Y") {
        Write-Host "Operation cancelled." -ForegroundColor Yellow
        exit 0
    }
}

# Function to get language pack directories
function Get-LanguagePackDirectories {
    param([string]$BasePath)
    
    $langDirs = @()
    
    if (Test-Path $BasePath) {
        Get-ChildItem $BasePath -Directory -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
            $dirName = $_.Name
            # Check if directory name matches language pack pattern (e.g., es-ES, fr-FR, de-DE, etc.)
            if ($dirName -match '^[a-z]{2}(-[A-Z]{2})?$' -and $dirName -notin $KeepLanguages) {
                $langDirs += $_.FullName
            }
        }
    }
    
    return $langDirs
}

# Function to calculate directory size
function Get-DirectorySize {
    param([string]$Path)
    
    try {
        $size = (Get-ChildItem $Path -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
        return [math]::Round($size / 1MB, 2)
    }
    catch {
        return 0
    }
}

# Function to remove language pack directory
function Remove-LanguagePack {
    param([string]$Path, [string]$Description)
    
    $size = Get-DirectorySize $Path
    Write-Host "Removing: $Description" -ForegroundColor Yellow
    Write-Host "  Path: $Path" -ForegroundColor Gray
    Write-Host "  Size: $size MB" -ForegroundColor Gray
    
    if (-not $WhatIf) {
        try {
            Remove-Item $Path -Recurse -Force -ErrorAction Stop
            Write-Host "  ✓ Removed successfully" -ForegroundColor Green
            return $size
        }
        catch {
            Write-Host "  ✗ Failed to remove: $($_.Exception.Message)" -ForegroundColor Red
            return 0
        }
    }
    else {
        Write-Host "  [WHAT-IF] Would remove $size MB" -ForegroundColor Magenta
        return $size
    }
}

# Initialize counters
$totalRemoved = 0
$totalSize = 0
$removedCount = 0

Write-Host "`n=== Scanning for Language Packs ===" -ForegroundColor Cyan

# Windows Store Apps (WindowsApps)
$windowsAppsPath = "C:\Program Files\WindowsApps"
Write-Host "`nScanning Windows Store Apps..." -ForegroundColor Yellow
$windowsAppsLangs = Get-LanguagePackDirectories $windowsAppsPath

if ($windowsAppsLangs.Count -gt 0) {
    Write-Host "Found $($windowsAppsLangs.Count) language pack directories in Windows Store Apps" -ForegroundColor Green
    foreach ($langDir in $windowsAppsLangs) {
        $appName = Split-Path (Split-Path $langDir -Parent) -Leaf
        $langName = Split-Path $langDir -Leaf
        $description = "Windows Store App ($appName) - $langName"
        $size = Remove-LanguagePack $langDir $description
        $totalSize += $size
        $removedCount++
    }
}
else {
    Write-Host "No non-English language packs found in Windows Store Apps" -ForegroundColor Gray
}

# Metro Apps in Program Files
$programFilesPaths = @("C:\Program Files", "C:\Program Files (x86)")
foreach ($pfPath in $programFilesPaths) {
    Write-Host "`nScanning $pfPath..." -ForegroundColor Yellow
    $metroLangs = Get-LanguagePackDirectories $pfPath
    
    if ($metroLangs.Count -gt 0) {
        Write-Host "Found $($metroLangs.Count) language pack directories in $pfPath" -ForegroundColor Green
        foreach ($langDir in $metroLangs) {
            $appName = Split-Path (Split-Path $langDir -Parent) -Leaf
            $langName = Split-Path $langDir -Leaf
            $description = "Metro App ($appName) - $langName"
            $size = Remove-LanguagePack $langDir $description
            $totalSize += $size
            $removedCount++
        }
    }
    else {
        Write-Host "No non-English language packs found in $pfPath" -ForegroundColor Gray
    }
}

# Windows Language Features (Aggressive)
Write-Host "`n=== Scanning Windows Language Features ===" -ForegroundColor Cyan

$languageFeaturePaths = @(
    "C:\Windows\System32\config\systemprofile\AppData\Local\Microsoft\Windows\INetCache",
    "C:\Windows\System32\config\systemprofile\AppData\Local\Microsoft\Windows\WebCache",
    "C:\Windows\System32\config\systemprofile\AppData\Local\Microsoft\Windows\History",
    "C:\Windows\System32\config\systemprofile\AppData\Local\Microsoft\Windows\Temporary Internet Files"
)

foreach ($path in $languageFeaturePaths) {
    if (Test-Path $path) {
        Write-Host "Clearing cache: $path" -ForegroundColor Yellow
        if (-not $WhatIf) {
            try {
                Remove-Item "$path\*" -Recurse -Force -ErrorAction SilentlyContinue
                Write-Host "  ✓ Cleared successfully" -ForegroundColor Green
            }
            catch {
                Write-Host "  ✗ Failed to clear: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        else {
            Write-Host "  [WHAT-IF] Would clear cache" -ForegroundColor Magenta
        }
    }
}

# Additional aggressive cleanup
Write-Host "`n=== Additional Aggressive Cleanup ===" -ForegroundColor Cyan

$additionalCleanupPaths = @(
    "C:\Windows\System32\config\systemprofile\AppData\Local\Microsoft\Windows\Explorer",
    "C:\Windows\System32\config\systemprofile\AppData\Local\Microsoft\Windows\INetCache\Low",
    "C:\Windows\System32\config\systemprofile\AppData\Local\Microsoft\Windows\WebCache\Low"
)

foreach ($path in $additionalCleanupPaths) {
    if (Test-Path $path) {
        Write-Host "Clearing additional cache: $path" -ForegroundColor Yellow
        if (-not $WhatIf) {
            try {
                Remove-Item "$path\*" -Recurse -Force -ErrorAction SilentlyContinue
                Write-Host "  ✓ Cleared successfully" -ForegroundColor Green
            }
            catch {
                Write-Host "  ✗ Failed to clear: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        else {
            Write-Host "  [WHAT-IF] Would clear cache" -ForegroundColor Magenta
        }
    }
}

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
if ($WhatIf) {
    Write-Host "WHAT-IF MODE: No files were actually deleted" -ForegroundColor Magenta
    Write-Host "Would have removed: $removedCount language pack directories" -ForegroundColor Yellow
    Write-Host "Would have freed: $([math]::Round($totalSize, 2)) MB" -ForegroundColor Yellow
}
else {
    Write-Host "Successfully removed: $removedCount language pack directories" -ForegroundColor Green
    Write-Host "Freed up: $([math]::Round($totalSize, 2)) MB" -ForegroundColor Green
}

# Check current disk space
$diskSpace = Get-WmiObject -Class Win32_LogicalDisk | Where-Object {$_.DeviceID -eq "C:"} | Select-Object @{Name="FreeSpace(GB)";Expression={[math]::Round($_.FreeSpace/1GB, 2)}}
Write-Host "Current free space on C: $($diskSpace.'FreeSpace(GB)') GB" -ForegroundColor Cyan

Write-Host "`nAggressive language pack cleanup completed!" -ForegroundColor Green
if (-not $WhatIf) {
    Write-Host "You may need to restart your computer for all changes to take effect." -ForegroundColor Yellow
}
