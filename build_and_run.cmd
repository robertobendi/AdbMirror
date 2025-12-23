@echo off
setlocal

rem Go to the project folder (relative to this script)
cd /d "%~dp0AdbMirror"

if not exist "AdbMirror.csproj" (
  echo AdbMirror.csproj not found in %cd%
  exit /b 1
)

echo Building AdbMirror...
dotnet build
if errorlevel 1 (
  echo Build failed.
  exit /b %errorlevel%
)

echo Running AdbMirror...
dotnet run

endlocal


