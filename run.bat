@echo off
title Emuzic Music Player Launcher
color 0A
echo.
echo ========================================
echo    🎵 Emuzic Music Player Launcher 🎵
echo ========================================
echo.
echo Starting Emuzic Music Player...
echo.

cd /d "%~dp0"

if not exist "bin\Release\net6.0-windows\Emuzic.exe" (
    echo Building Emuzic...
    dotnet build -c Release
    echo.
)

if exist "bin\Release\net6.0-windows\Emuzic.exe" (
    echo Launching Emuzic.exe...
    start "" "bin\Release\net6.0-windows\Emuzic.exe"
    echo.
    echo Emuzic is now running! Enjoy your music! 🎶
) else (
    echo Error: Emuzic.exe not found!
    echo Please run 'dotnet build -c Release' first.
)

echo.
pause
