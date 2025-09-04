@echo off
echo ========================================
echo    Language Pack Cleanup Launcher
echo ========================================
echo.
echo This will run the language pack cleanup script
echo with Administrator privileges.
echo.
echo The script will remove non-English language packs
echo from Windows Store Apps and Metro apps to free up space.
echo.
pause

powershell.exe -ExecutionPolicy Bypass -File "%~dp0Remove-LanguagePacks.ps1" -WhatIf

echo.
echo ========================================
echo    What-If Mode Completed
echo ========================================
echo.
echo The script ran in WHAT-IF mode to show you
echo what would be removed without actually deleting anything.
echo.
echo To actually remove the language packs, run:
echo    powershell.exe -ExecutionPolicy Bypass -File "Remove-LanguagePacks.ps1"
echo.
echo Or to run without confirmation:
echo    powershell.exe -ExecutionPolicy Bypass -File "Remove-LanguagePacks.ps1" -Force
echo.
pause
