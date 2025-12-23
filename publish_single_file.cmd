@echo off
REM Publish AdbMirror as a single-file, self-contained executable

echo Publishing AdbMirror as single-file executable...
echo.

REM Prepare resources first
call "%~dp0prepare_resources.cmd"

cd /d "%~dp0AdbMirror"

REM Clean previous publish
if exist "bin\Release\net8.0-windows\win-x64\publish" (
    rmdir /s /q "bin\Release\net8.0-windows\win-x64\publish"
)

REM Publish as single-file, self-contained
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "bin\Release\net8.0-windows\win-x64\publish"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Publish failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo Publish complete!
echo.
echo Output location:
echo %CD%\bin\Release\net8.0-windows\win-x64\publish
echo.
echo The AdbMirror.exe is a single-file executable with ALL dependencies embedded!
echo platform-tools and scrcpy are extracted automatically at runtime.
echo You can distribute just the AdbMirror.exe file.
echo ========================================
echo.

echo Opening publish folder...
start explorer.exe "bin\Release\net8.0-windows\win-x64\publish"

pause

