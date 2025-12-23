@echo off
REM Publish as single-file release executable
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

echo Publishing AdbMirror...
"%DOTNET_CMD%" publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:SatelliteResourceLanguages=en -o "bin\Release\net8.0-windows\win-x64\publish"

if %ERRORLEVEL% NEQ 0 (
    echo Publish failed!
    pause
    exit /b 1
)

echo.
echo Cleaning publish folder (keeping only exe)...
pushd "bin\Release\net8.0-windows\win-x64\publish"
for %%f in (*) do if /i not "%%f"=="AdbMirror.exe" del /f /q "%%f"
for /d %%d in (*) do rmdir /s /q "%%d"
popd

echo.
echo Opening publish folder...
start explorer.exe "bin\Release\net8.0-windows\win-x64\publish"

