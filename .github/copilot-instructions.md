# Screenshotter - Copilot Instructions

## Project Overview

Screenshotter is a dual-platform application that enables taking screenshots on a Windows PC remotely from an iPhone. The project consists of:

1. **Windows App** (`windows/Screenshotter.Windows/`) - A .NET 10 WinUI 3 packaged system tray application
2. **iOS App** (`ios/Screenshotter/`) - A SwiftUI application targeting iOS 16+

## Architecture

```
┌─────────────────┐     WiFi (HTTP)      ┌──────────────────────────────┐
│   iPhone App    │◄──────────────────►  │  Windows System Tray App     │
│   (SwiftUI)     │   Port 5000          │  (WinUI 3 + Minimal API)     │
│                 │                       │                              │
│  - QR Scanner   │   GET /api/info      │  - QR Code Display           │
│  - Screenshot   │   POST /api/screenshot│  - Screen Capture            │
│  - Photo Save   │                       │  - API Server                │
└─────────────────┘                       └──────────────────────────────┘
```

## Technology Stack

### Windows App
- **.NET 10** with WinUI 3 (Windows App SDK)
- **H.NotifyIcon.WinUI** for system tray functionality
- **QRCoder** for QR code generation
- **ASP.NET Core Minimal API** for HTTP endpoints
- **System.Drawing.Common** for screen capture

### iOS App
- **Swift 6** with strict concurrency checking
- **SwiftUI** for UI
- **AVFoundation** for QR code scanning
- **PhotoKit** for saving to Photos library

## Key Files

### Windows
| File | Purpose |
|------|---------|
| `Services/ApiServerService.cs` | HTTP server on port 5000 |
| `Services/ScreenshotService.cs` | Primary screen capture |
| `Services/NetworkService.cs` | Local IP detection |
| `Services/QrCodeService.cs` | QR code PNG generation |
| `MainWindow.xaml` | System tray popup UI |

### iOS
| File | Purpose |
|------|---------|
| `Views/QRScannerView.swift` | QR code scanning with AVCaptureSession |
| `Views/MainView.swift` | Main screenshot trigger UI |
| `Services/ScreenshotService.swift` | HTTP calls and photo saving |
| `Services/ConnectionManager.swift` | Connection state management |

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/info` | Returns server status, IP, port, timestamp |
| POST | `/api/screenshot` | Captures primary screen, returns PNG bytes |
| GET | `/api/health` | Health check |

## QR Code Format

The QR code encodes a JSON payload:
```json
{"ip":"192.168.1.x","port":5000}
```

## Build Instructions

### Windows
```bash
cd windows/Screenshotter.Windows
dotnet build
```
Default platform is x64. For other platforms:
```bash
dotnet build -r win-arm64
```

### iOS
Open `ios/Screenshotter/Screenshotter.xcodeproj` in Xcode 16+ and build.

## Development Notes

1. **Firewall**: Windows Firewall must allow inbound connections on port 5000
2. **Network**: Both devices must be on the same local network
3. **MSIX Packaging**: The Windows app uses MSIX for deployment, requires signing for installation
4. **iOS Permissions**: Camera and Photo Library permissions required

## Common Patterns

### Adding new API endpoints
Add routes in `ApiServerService.cs` using the Minimal API pattern:
```csharp
app.MapGet("/api/new-endpoint", () => Results.Ok(new { ... }));
```

### Adding iOS views
Place new views in `Screenshotter/Views/` following SwiftUI conventions with `@MainActor` where needed.

### State persistence
- Windows: Registry for startup setting
- iOS: `@AppStorage` for paired IP
