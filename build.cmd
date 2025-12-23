@echo off
REM Simple build and run script
setlocal

REM Find dotnet
set "DOTNET_CMD=dotnet"
where dotnet >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    if exist "%ProgramFiles%\dotnet\dotnet.exe" (
        set "DOTNET_CMD=%ProgramFiles%\dotnet\dotnet.exe"
    ) else if exist "%ProgramFiles(x86)%\dotnet\dotnet.exe" (
        set "DOTNET_CMD=%ProgramFiles(x86)%\dotnet\dotnet.exe"
    ) else (
        echo ERROR: dotnet.exe not found. Please install .NET SDK or add it to PATH.
        pause
        exit /b 1
    )
)

cd /d "%~dp0AdbMirror"

echo Building AdbMirror...
"%DOTNET_CMD%" build

if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Running AdbMirror...
start "" "%DOTNET_CMD%" run

echo.
echo Opening build folder...
timeout /t 2 /nobreak >nul
start explorer.exe "bin\Debug\net8.0-windows"

