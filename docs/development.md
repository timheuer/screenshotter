# Development Guide

This document contains technical information for developers contributing to Screenshotter.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              LOCAL NETWORK (WiFi)                           │
│                                                                             │
│  ┌───────────────────┐                         ┌──────────────────────────┐ │
│  │     iPhone        │                         │      Windows PC          │ │
│  │  ┌─────────────┐  │      HTTP Request       │  ┌──────────────────┐   │ │
│  │  │   SwiftUI   │  │  ───────────────────►   │  │  WinUI 3 Tray    │   │ │
│  │  │  iOS App    │  │   POST /screenshot      │  │      App         │   │ │
│  │  │             │  │                         │  │  ┌────────────┐  │   │ │
│  │  │ ┌─────────┐ │  │                         │  │  │ QR Code    │  │   │ │
│  │  │ │  Scan   │ │  │      QR Code Scan       │  │  │ Display    │  │   │ │
│  │  │ │QR Code  │─┼──┼──── (Initial Pairing)   │  │  └────────────┘  │   │ │
│  │  │ └─────────┘ │  │                         │  │                  │   │ │
│  │  │             │  │                         │  │  ┌────────────┐  │   │ │
│  │  │ ┌─────────┐ │  │                         │  │  │ Minimal    │  │   │ │
│  │  │ │Capture  │ │  │  ◄───────────────────   │  │  │ API Server │  │   │ │
│  │  │ │ Button  │ │  │     200 OK + Image      │  │  │ Port 5000  │  │   │ │
│  │  │ └─────────┘ │  │                         │  │  └────────────┘  │   │ │
│  │  └─────────────┘  │                         │  └──────────────────┘   │ │
│  └───────────────────┘                         └──────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### How It Works

1. **Pairing**: The Windows app displays a QR code containing the PC's local IP address and port
2. **Scanning**: The iOS app scans this QR code to establish the connection endpoint
3. **Triggering**: Tap the capture button on your iPhone to send an HTTP request to the Windows app
4. **Capture**: The Windows app captures the screen and returns the screenshot as PNG

## Project Structure

```
screenshotter/
├── ios/
│   └── Screenshotter/
│       └── Screenshotter/
│           ├── ScreenshotterApp.swift    # App entry point
│           ├── ContentView.swift         # Root view
│           ├── Views/
│           │   ├── MainView.swift        # Main capture interface
│           │   ├── QRScannerView.swift   # QR code scanning
│           │   └── SettingsView.swift    # Settings screen
│           ├── Services/
│           │   ├── ScreenshotService.swift   # HTTP client
│           │   └── ConnectionManager.swift   # Connection state
│           └── Components/
│               └── ToastView.swift       # Toast notifications
├── windows/
│   └── Screenshotter.Windows/
│       ├── App.xaml.cs                   # Application entry point
│       ├── MainWindow.xaml               # QR code display window
│       └── Services/
│           ├── ApiServerService.cs       # Minimal API server
│           ├── ScreenshotService.cs      # Screen capture
│           ├── QrCodeService.cs          # QR code generation
│           ├── NetworkService.cs         # Local IP detection
│           └── TrayIconService.cs        # System tray
├── docs/
│   ├── privacy.md                        # Privacy policy
│   └── development.md                    # This file
└── .github/
    └── workflows/
        ├── build.yml                     # CI build (iOS + Windows)
        └── publish.yml                   # TestFlight deployment
```

## Prerequisites

### Windows Development

| Requirement | Version |
|------------|---------|
| Windows | 10 (1809+) or Windows 11 |
| .NET SDK | 10.0 or later |
| Visual Studio | 2022 (17.8+) with WinUI 3 workload |
| Windows App SDK | 1.6 or later |

### iOS Development

| Requirement | Version |
|------------|---------|
| macOS | Ventura 14.0+ |
| Xcode | 16.0 or later |
| iOS Deployment Target | iOS 17.0+ |
| Swift | 6.0 |

## Building from Source

### Windows App

```powershell
cd windows/Screenshotter.Windows
dotnet restore
dotnet build -c Release
```

For a published build:
```powershell
dotnet publish -c Release -r win-x64
```

### iOS App

Open the project in Xcode:
```bash
cd ios/Screenshotter
open Screenshotter.xcodeproj
```

Or build from command line:
```bash
xcodebuild -project Screenshotter.xcodeproj \
  -scheme Screenshotter \
  -configuration Release \
  -destination 'generic/platform=iOS'
```

## API Endpoints

The Windows app exposes these HTTP endpoints on port 5000:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/info` | Returns server status, IP, port, timestamp |
| POST | `/api/screenshot` | Captures primary screen, returns PNG bytes |
| GET | `/api/health` | Health check endpoint |

### QR Code Format

The QR code encodes a JSON payload:
```json
{"ip":"192.168.1.x","port":5000}
```

## Firewall Configuration

The Windows app needs inbound connections on port 5000:

```powershell
# Add firewall rule (run as Administrator)
netsh advfirewall firewall add rule name="Screenshotter" dir=in action=allow protocol=TCP localport=5000

# Remove the rule
netsh advfirewall firewall delete rule name="Screenshotter"
```

## CI/CD

### Build Workflow

The build workflow (`.github/workflows/build.yml`) runs iOS and Windows builds in parallel on:
- Push to `main` branch
- Pull requests to `main`
- Manual dispatch

### Publish Workflow

The publish workflow (`.github/workflows/publish.yml`) deploys to TestFlight on:
- Push of `v*` tags (e.g., `v1.0.0`)
- Manual dispatch

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `APPSTORE_CERTIFICATE_P12` | Base64-encoded Apple Distribution certificate |
| `APPSTORE_CERTIFICATE_P12_PASSWORD` | Certificate password |
| `APPSTORE_ISSUER_ID` | App Store Connect API Issuer ID |
| `APPSTORE_KEY_ID` | App Store Connect API Key ID |
| `APPSTORE_PRIVATE_KEY` | App Store Connect API Private Key (.p8 content) |

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Troubleshooting

### Port Already in Use

```powershell
# Find what's using port 5000
netstat -ano | findstr :5000
```

### Network Diagnostics

```powershell
# Find your PC's local IP address
ipconfig | findstr /i "IPv4"

# Test if the port is accessible locally
Test-NetConnection -ComputerName localhost -Port 5000

# Check if the server is running
curl http://localhost:5000/api/health
```

### Windows App Won't Start

1. Ensure .NET 10 Desktop Runtime is installed:
   ```powershell
   winget install Microsoft.DotNet.DesktopRuntime.10
   ```

2. Reinstall the Windows App SDK:
   ```powershell
   winget install Microsoft.WindowsAppSDK
   ```
