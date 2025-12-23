# Publish AdbMirror as a single-file, self-contained executable

Write-Host "Publishing AdbMirror as single-file executable..." -ForegroundColor Cyan
Write-Host ""

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path

# Prepare resources first
Write-Host "Preparing resources..." -ForegroundColor Cyan
& (Join-Path $scriptPath "prepare_resources.cmd")

$projectPath = Join-Path $scriptPath "AdbMirror"

Push-Location $projectPath

# Clean previous publish
$publishPath = "bin\Release\net8.0-windows\win-x64\publish"
if (Test-Path $publishPath) {
    Remove-Item -Path $publishPath -Recurse -Force
}

# Publish as single-file, self-contained
dotnet publish -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishPath

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Publish failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Publish complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Output location:" -ForegroundColor Cyan
Write-Host (Resolve-Path $publishPath).Path
Write-Host ""
Write-Host "The AdbMirror.exe is a single-file executable with ALL dependencies embedded!" -ForegroundColor Green
Write-Host "platform-tools and scrcpy are extracted automatically at runtime." -ForegroundColor Green
Write-Host "You can distribute just the AdbMirror.exe file." -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

Write-Host "Opening publish folder..." -ForegroundColor Cyan
$fullPublishPath = (Resolve-Path $publishPath).Path
Start-Process explorer.exe -ArgumentList $fullPublishPath

Pop-Location

