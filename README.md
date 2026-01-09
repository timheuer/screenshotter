# 📸 Screenshotter

Take screenshots on your Windows PC remotely from your iPhone. Perfect for presentations, demos, streaming setups, or any time you need wireless screenshot control.

## ✨ Features

- **Instant Capture** — One tap on your iPhone captures your Windows screen
- **Multi-Monitor Support** — Choose a specific monitor or capture all displays
- **Separate Images** — "All Monitors" saves each display as its own photo
- **Easy Pairing** — Scan a QR code to connect, no manual IP entry needed
- **Saves to Photos** — Screenshots save directly to your iPhone's photo library with metadata
- **Runs in Background** — Windows app stays quietly in your system tray
- **No Account Required** — Works entirely on your local network, no cloud services
- **Privacy First** — No data collection, no tracking, no analytics

## 📱 Requirements

| Device | Requirements |
|--------|-------------|
| **iPhone** | iOS 17.0 or later |
| **Windows PC** | Windows 10 (1809+) or Windows 11, .NET 10 |
| **Network** | Both devices on the same WiFi network |

## 🚀 Quick Start

### 1. Install the Apps

- **Windows**: Download from [Releases](https://github.com/timheuer/screenshotter/releases) or build from source
- **iPhone**: Download from the App Store *(coming soon)* or build with Xcode

### 2. Start the Windows App

Launch Screenshotter on your PC. It will appear in your system tray (bottom-right corner). Click the icon to show the QR code.

### 3. Pair Your iPhone

Open Screenshotter on your iPhone and tap **Scan QR Code**. Point your camera at the QR code on your Windows screen.

### 4. Capture

Tap the capture button on your iPhone. Your Windows screen is captured and saved to your Photos.

**Multi-Monitor Setup:** If you have multiple displays, use the monitor picker to select which screen to capture, or choose "All Monitors" to save each display as a separate image.

## 🔒 Privacy

Screenshotter operates entirely on your local network. No data is sent to external servers, no accounts are required, and no information is collected. See our [Privacy Policy](docs/privacy.md) for details.

## 🛠️ Building from Source

See the [Development Guide](docs/development.md) for build instructions and technical details.

### Quick Build

**Windows:**

```powershell
cd windows/Screenshotter.Windows
dotnet build
```

**iOS:**

```bash
cd ios/Screenshotter
open Screenshotter.xcodeproj
# Build and run from Xcode
```

## 🤝 Contributing

Contributions are welcome! Please see the [Development Guide](docs/development.md) for details on the project structure and how to get started.

## 📄 License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.

## 🙏 Acknowledgments

- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) for WinUI 3
- [QRCoder](https://github.com/codebude/QRCoder) for QR code generation
- [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon) for system tray support

---

<p align="center">
  Made with ❤️ by <a href="https://github.com/timheuer">Tim Heuer</a> and GitHub Copilot
</p>
