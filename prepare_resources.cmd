@echo off
REM Prepare platform-tools and scrcpy folders as zip files for embedding

echo Preparing resources for embedding...
echo.

cd /d "%~dp0"

REM Create Resources directory if it doesn't exist
if not exist "Resources" mkdir Resources

REM Zip platform-tools if it exists
if exist "AdbMirror\platform-tools" (
    echo Creating platform-tools.zip...
    if not exist "AdbMirror\Resources" mkdir "AdbMirror\Resources"
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path 'AdbMirror\platform-tools\*' -DestinationPath 'AdbMirror\Resources\platform-tools.zip' -Force"
    if %ERRORLEVEL% EQU 0 (
        echo platform-tools.zip created successfully.
    ) else (
        echo ERROR: Failed to create platform-tools.zip
    )
) else (
    echo WARNING: platform-tools folder not found, skipping...
)

REM Zip scrcpy if it exists
if exist "AdbMirror\scrcpy" (
    echo Creating scrcpy.zip...
    if not exist "AdbMirror\Resources" mkdir "AdbMirror\Resources"
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path 'AdbMirror\scrcpy\*' -DestinationPath 'AdbMirror\Resources\scrcpy.zip' -Force"
    if %ERRORLEVEL% EQU 0 (
        echo scrcpy.zip created successfully.
    ) else (
        echo ERROR: Failed to create scrcpy.zip
    )
) else (
    echo WARNING: scrcpy folder not found, skipping...
)

echo.
echo Resource preparation complete!
echo.

