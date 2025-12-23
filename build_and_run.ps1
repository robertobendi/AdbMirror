param(
    [switch]$NoRun  # If set, only build, don't run
)

Write-Host "Switching to project directory..." -ForegroundColor Cyan
Set-Location -Path "$PSScriptRoot\AdbMirror"

if (-not (Test-Path "AdbMirror.csproj")) {
    Write-Error "AdbMirror.csproj not found in $(Get-Location)"
    exit 1
}

Write-Host "Building AdbMirror..." -ForegroundColor Cyan
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit $LASTEXITCODE
}

if (-not $NoRun) {
    Write-Host "Running AdbMirror..." -ForegroundColor Cyan
    dotnet run
}


