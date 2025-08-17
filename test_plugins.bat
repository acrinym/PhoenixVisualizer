@echo off
echo Building Winamp Plugin Test...
echo.

REM Try to build the test program
dotnet build TestWinampPlugins.cs --output bin/test

if %ERRORLEVEL% NEQ 0 (
    echo Build failed. Trying alternative approach...
    echo.
    echo Please ensure you have .NET SDK installed and run:
    echo   dotnet new console --name TestWinampPlugins
    echo   copy TestWinampPlugins.cs TestWinampPlugins\
    echo   cd TestWinampPlugins
    echo   dotnet run
    echo.
    pause
    exit /b 1
)

echo Build successful!
echo.
echo Running plugin test...
echo.

cd bin/test
TestWinampPlugins.exe

echo.
echo Test completed.
pause
