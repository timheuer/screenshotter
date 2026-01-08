# Screenshotter Windows App - Asset Placeholder

This directory should contain the following image assets for the packaged WinUI 3 app:

## Required Assets

Create PNG images with these exact names and dimensions:

| File Name | Dimensions | Description |
|-----------|------------|-------------|
| `SplashScreen.scale-200.png` | 1240 x 600 px | Splash screen shown during app launch |
| `LockScreenLogo.scale-200.png` | 48 x 48 px | Lock screen badge icon |
| `Square150x150Logo.scale-200.png` | 300 x 300 px | Medium tile icon |
| `Square44x44Logo.scale-200.png` | 88 x 88 px | Small tile/taskbar icon |
| `Square44x44Logo.targetsize-24_altform-unplated.png` | 24 x 24 px | Unplated taskbar icon |
| `StoreLogo.png` | 50 x 50 px | Store listing logo |
| `Wide310x150Logo.scale-200.png` | 620 x 300 px | Wide tile icon |

## Quick Generation

You can generate placeholder images using PowerShell:

```powershell
# Creates simple placeholder PNGs (requires .NET)
Add-Type -AssemblyName System.Drawing

$assets = @{
    "SplashScreen.scale-200.png" = @(1240, 600)
    "LockScreenLogo.scale-200.png" = @(48, 48)
    "Square150x150Logo.scale-200.png" = @(300, 300)
    "Square44x44Logo.scale-200.png" = @(88, 88)
    "Square44x44Logo.targetsize-24_altform-unplated.png" = @(24, 24)
    "StoreLogo.png" = @(50, 50)
    "Wide310x150Logo.scale-200.png" = @(620, 300)
}

foreach ($asset in $assets.GetEnumerator()) {
    $bmp = New-Object System.Drawing.Bitmap($asset.Value[0], $asset.Value[1])
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(100, 149, 237)) # Cornflower blue
    $bmp.Save("$PSScriptRoot\$($asset.Key)")
    $g.Dispose()
    $bmp.Dispose()
}
```
