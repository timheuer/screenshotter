# Run Screenshotter in development mode
# Builds and launches the unpackaged app

$projectDir = $PSScriptRoot
$configuration = "Debug"
$platform = "x64"

Write-Host "Building Screenshotter..." -ForegroundColor Cyan

# Build the project
Push-Location $projectDir
dotnet build -c $configuration -p:Platform=$platform
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location

# Find and launch the exe (in the win-x64 subfolder for self-contained builds)
$exePath = Join-Path $projectDir "bin\$platform\$configuration\net10.0-windows10.0.22621.0\win-$platform\Screenshotter.Windows.exe"

if (-not (Test-Path $exePath)) {
    # Try path without win-x64 subfolder
    $exePath = Join-Path $projectDir "bin\$platform\$configuration\net10.0-windows10.0.22621.0\Screenshotter.Windows.exe"
}

if (-not (Test-Path $exePath)) {
    # Try alternate path without platform folder
    $exePath = Join-Path $projectDir "bin\$configuration\net10.0-windows10.0.22621.0\Screenshotter.Windows.exe"
}

if (Test-Path $exePath) {
    Write-Host "Launching Screenshotter from: $exePath" -ForegroundColor Green
    Start-Process $exePath -WorkingDirectory (Split-Path $exePath)
}
else {
    Write-Host "Exe not found at: $exePath" -ForegroundColor Red
    Write-Host "Check the build output for the correct path." -ForegroundColor Yellow
}
