# Run Screenshotter in development mode
# This registers the MSIX package and launches the app

$projectDir = $PSScriptRoot
$configuration = "Debug"
$platform = "x64"

Write-Host "Building and launching Screenshotter..." -ForegroundColor Cyan

# Build the project
Push-Location $projectDir
dotnet build -c $configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location

# Find and register the MSIX package
$appxPath = Join-Path $projectDir "bin\$platform\$configuration\net10.0-windows10.0.22621.0\win-$platform\AppX"
$manifestPath = Join-Path $appxPath "AppxManifest.xml"

if (Test-Path $manifestPath) {
    Write-Host "Registering package from: $appxPath" -ForegroundColor Yellow
    Add-AppxPackage -Register $manifestPath -ForceApplicationShutdown
    
    # Launch the app
    Write-Host "Launching Screenshotter..." -ForegroundColor Green
    Start-Process "shell:AppsFolder\Screenshotter_h3c97v1z0qnmj!App"
} else {
    Write-Host "AppX manifest not found at: $manifestPath" -ForegroundColor Red
    Write-Host "Try building with: dotnet build -c Debug" -ForegroundColor Yellow
}
