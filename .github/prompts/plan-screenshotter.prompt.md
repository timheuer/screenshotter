## Plan: iOS Remote Screenshot Controller for Windows

Build a .NET 10 WinUI 3 system tray app with QR-code pairing and a SwiftUI iPhone app that scans to pair, triggers primary monitor screenshots, and saves to Photos. Includes placeholder icons and a README for setup documentation.

### Steps

1. **Create README.md** — Document project overview, architecture diagram (ASCII), prerequisites (.NET 10 SDK, Xcode 16+), setup instructions for both apps, first-time pairing walkthrough, and troubleshooting section. Update iteratively as features are built.

2. **Create .NET 10 WinUI 3 project with system tray** — New WinUI 3 packaged app (`windows/Screenshotter.Windows/`) using `H.NotifyIcon.WinUI`. Tray icon left-click shows popup with: QR code image, IP label, status text, "Refresh IP" button, "Start with Windows" toggle. Add placeholder `.ico` icon (simple camera glyph or colored square).

3. **Implement IP detection and QR generation** — `Services/NetworkService.cs` with `GetLocalIPAddress()` using `NetworkInterface` APIs. `Services/QrCodeService.cs` using `QRCoder` to encode `{"ip":"192.168.x.x","port":5000}` as PNG bitmap.

4. **Add background Minimal API server** — `Services/ApiServerService.cs` hosting `WebApplication` on `http://0.0.0.0:5000`. Endpoints: `GET /api/info` → JSON status, `POST /api/screenshot` → capture `Screen.PrimaryScreen.Bounds`, return PNG bytes.

5. **Create SwiftUI iOS project** — Xcode project `ios/Screenshotter/` targeting iOS 16+. Add placeholder `AppIcon` asset (solid color square). Configure Info.plist with camera, photos, and local network usage descriptions.

6. **Build QRScannerView** — `UIViewControllerRepresentable` wrapping `AVCaptureSession` for QR scanning. Parse JSON, validate via `GET /api/info`, save IP to `@AppStorage("pairedIP")` on success.

7. **Build MainView** — Connection status indicator, large "Take Screenshot" button, thumbnail of last capture. Gear icon navigates to `SettingsView`.

8. **Build SettingsView** — Display paired IP, "Re-scan QR Code" button, manual IP text field with "Test Connection" button.

9. **Implement ScreenshotService** — Async methods for `captureScreenshot()` (POST to Windows, returns `UIImage`) and `saveToPhotos(_:)` (PhotoKit integration).

10. **Add toast notifications and error handling** — Green slide-in toast for "Saved to Photos!", alert dialogs for connection/save errors with retry options.

### Further Considerations

1. **Shall I proceed to implementation?** The plan covers all requirements — ready to start with the README and then the Windows app, or would you prefer iOS first?
