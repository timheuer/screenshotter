# 📸 iOS Remote Screenshot Controller

A seamless remote screenshot solution that lets you trigger Windows PC screenshots from your iPhone. Perfect for presentations, demos, streaming setups, or any scenario where you need wireless screenshot control.

## 🎯 Overview

This project consists of two applications working together:

- **Windows System Tray App** - A .NET 10 WinUI 3 application that runs quietly in your system tray, displays a QR code for easy pairing, and hosts a minimal web server to receive screenshot commands
- **iOS Companion App** - A SwiftUI app that scans the pairing QR code and provides a simple interface to trigger screenshots on your paired Windows PC

## 🏗️ Architecture

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
4. **Capture**: The Windows app captures the screen and saves/responds with the screenshot

## 📋 Prerequisites

### Windows PC

| Requirement | Version |
|------------|---------|
| Windows | 10 (1809+) or Windows 11 |
| .NET SDK | 10.0 or later |
| Visual Studio | 2022 (17.8+) with WinUI 3 workload |
| Windows App SDK | 1.5 or later |

### macOS (for iOS development)

| Requirement | Version |
|------------|---------|
| macOS | Ventura 14.0+ |
| Xcode | 16.0 or later |
| iOS Deployment Target | iOS 16.0+ |

### Network Requirements

- Both devices must be connected to the **same local network** (WiFi)
- Port **5000** must be accessible on the Windows PC (see [Firewall Configuration](#firewall-configuration))

## 🚀 Setup Instructions

### Windows App Setup

1. **Clone the repository**
   ```powershell
   git clone https://github.com/yourusername/screenshotter.git
   cd screenshotter
   ```

2. **Open the solution in Visual Studio 2022**
   ```powershell
   start Screenshotter.sln
   ```

3. **Restore NuGet packages**
   ```powershell
   dotnet restore
   ```

4. **Build and run the application**
   - Set the Windows project as the startup project
   - Press `F5` or click **Start Debugging**
   - The app will minimize to the system tray

5. **Configure Windows Firewall** (if needed)
   ```powershell
   # Run as Administrator
   netsh advfirewall firewall add rule name="Screenshotter" dir=in action=allow protocol=TCP localport=5000
   ```

### iOS App Setup

1. **Navigate to the iOS project folder**
   ```bash
   cd screenshotter/ios
   ```

2. **Open the project in Xcode**
   ```bash
   open Screenshotter.xcodeproj
   ```
   Or if using Swift Package Manager:
   ```bash
   open Screenshotter.xcworkspace
   ```

3. **Configure signing**
   - Select the project in the Navigator
   - Go to **Signing & Capabilities**
   - Select your **Team** and update the **Bundle Identifier**

4. **Build and run**
   - Select your iPhone as the target device
   - Press `Cmd + R` or click the **Run** button
   - Trust the developer certificate on your iPhone if prompted:
     - Go to **Settings > General > VPN & Device Management**
     - Tap your developer certificate and select **Trust**

## 📱 First-Time Pairing Walkthrough

### Step 1: Start the Windows App

1. Launch the Screenshotter app on your Windows PC
2. The app will appear in your system tray (bottom-right corner)
3. Click the tray icon to open the pairing window
4. A QR code will be displayed containing your PC's connection details

### Step 2: Pair Your iPhone

1. Open the Screenshotter app on your iPhone
2. Tap **Scan QR Code** or the camera icon
3. Point your camera at the QR code displayed on your Windows PC
4. Wait for the confirmation vibration/sound

### Step 3: Verify Connection

1. You should see a **"Connected"** status on your iPhone
2. The Windows app will show a notification confirming the pairing
3. Your devices are now paired and ready!

### Step 4: Take Your First Screenshot

1. On your iPhone, tap the large **Capture** button
2. Your Windows PC will capture the current screen
3. The screenshot will be saved to your configured folder (default: `Pictures/Screenshots`)
4. A thumbnail preview may appear on your iPhone (if enabled)

## ⚙️ Configuration

### Windows App Settings

Access settings by right-clicking the tray icon and selecting **Settings**:

| Setting | Description | Default |
|---------|-------------|---------|
| Port | Web server port number | `5000` |
| Save Location | Screenshot save directory | `Pictures/Screenshots` |
| Image Format | PNG, JPEG, or BMP | `PNG` |
| Auto-start | Launch on Windows startup | `false` |
| Sound | Play sound on capture | `true` |

### iOS App Settings

Access settings from the gear icon in the app:

| Setting | Description | Default |
|---------|-------------|---------|
| Haptic Feedback | Vibrate on capture | `true` |
| Show Preview | Display thumbnail after capture | `true` |
| Keep Screen On | Prevent auto-lock | `false` |

## 🔧 Troubleshooting

### Common Issues

#### ❌ "Connection Failed" or "Unable to Connect"

**Cause**: Devices are not on the same network or firewall is blocking the connection.

**Solutions**:
1. Verify both devices are connected to the same WiFi network
2. Check that the Windows Firewall allows incoming connections on port 5000:
   ```powershell
   # Check if the rule exists
   netsh advfirewall firewall show rule name="Screenshotter"
   
   # Add the rule if missing (run as Administrator)
   netsh advfirewall firewall add rule name="Screenshotter" dir=in action=allow protocol=TCP localport=5000
   ```
3. Temporarily disable third-party antivirus/firewall to test
4. Try using a different port in the settings

#### ❌ QR Code Won't Scan

**Cause**: Camera permissions, lighting, or distance issues.

**Solutions**:
1. Ensure the iOS app has camera permissions:
   - Go to **Settings > Privacy & Security > Camera**
   - Enable access for Screenshotter
2. Clean your camera lens
3. Adjust the distance between your phone and the QR code
4. Improve lighting conditions
5. Try regenerating the QR code by clicking **Refresh** in the Windows app

#### ❌ "Port Already in Use" Error

**Cause**: Another application is using port 5000.

**Solutions**:
1. Find what's using the port:
   ```powershell
   netstat -ano | findstr :5000
   ```
2. Either close the conflicting application or change the Screenshotter port in settings

#### ❌ Screenshots Not Saving

**Cause**: Permission issues or invalid save path.

**Solutions**:
1. Verify the save directory exists and is writable
2. Check available disk space
3. Run the app with administrator privileges (right-click > Run as Administrator)
4. Reset the save location to the default folder

#### ❌ High Latency / Slow Response

**Cause**: Network congestion or weak WiFi signal.

**Solutions**:
1. Move closer to your WiFi router
2. Use the 5GHz WiFi band instead of 2.4GHz if available
3. Disconnect other bandwidth-heavy devices
4. Restart your router

#### ❌ App Crashes on Startup (Windows)

**Cause**: Missing dependencies or corrupted installation.

**Solutions**:
1. Ensure .NET 10 Desktop Runtime is installed:
   ```powershell
   winget install Microsoft.DotNet.DesktopRuntime.10
   ```
2. Reinstall the Windows App SDK:
   ```powershell
   winget install Microsoft.WindowsAppSDK
   ```
3. Delete the app data folder and restart:
   ```powershell
   Remove-Item -Recurse "$env:LOCALAPPDATA\Screenshotter"
   ```

### Firewall Configuration

#### Windows Defender Firewall

```powershell
# Allow incoming connections (run as Administrator)
netsh advfirewall firewall add rule name="Screenshotter" dir=in action=allow protocol=TCP localport=5000

# Remove the rule later if needed
netsh advfirewall firewall delete rule name="Screenshotter"
```

#### Third-Party Firewalls

If using a third-party firewall (Norton, McAfee, etc.), add an exception for:
- **Application**: `Screenshotter.exe`
- **Port**: `5000` (TCP, Inbound)
- **Network**: Private/Home networks only (recommended)

### Network Diagnostics

Run these commands to help diagnose network issues:

```powershell
# Find your PC's local IP address
ipconfig | findstr /i "IPv4"

# Test if the port is accessible locally
Test-NetConnection -ComputerName localhost -Port 5000

# Check if the server is running
curl http://localhost:5000/health
```

## 🛠️ Development

### Building from Source

#### Windows App
```powershell
cd src/Windows
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained
```

#### iOS App
```bash
cd src/iOS
xcodebuild -scheme Screenshotter -configuration Release -destination 'generic/platform=iOS'
```

### Running Tests

```powershell
# Windows unit tests
dotnet test src/Windows.Tests

# iOS tests (from Xcode or command line)
xcodebuild test -scheme Screenshotter -destination 'platform=iOS Simulator,name=iPhone 15'
```

## 📁 Project Structure

```
screenshotter/
├── src/
│   ├── Windows/                 # WinUI 3 Windows App
│   │   ├── App.xaml             # Application entry point
│   │   ├── MainWindow.xaml      # QR code display window
│   │   ├── TrayIcon.cs          # System tray functionality
│   │   ├── ScreenshotService.cs # Screen capture logic
│   │   └── WebServer/           # Minimal API server
│   │       └── ScreenshotApi.cs
│   └── iOS/                     # SwiftUI iOS App
│       ├── ScreenshotterApp.swift
│       ├── Views/
│       │   ├── ContentView.swift
│       │   ├── ScannerView.swift
│       │   └── CaptureButton.swift
│       └── Services/
│           └── NetworkService.swift
├── tests/
│   ├── Windows.Tests/
│   └── iOS.Tests/
├── docs/
├── README.md
└── LICENSE
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

```
MIT License

Copyright (c) 2026 Screenshotter Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## 🙏 Acknowledgments

- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) for WinUI 3 support
- [QRCoder](https://github.com/codebude/QRCoder) for QR code generation
- [ASP.NET Core Minimal APIs](https://docs.microsoft.com/aspnet/core/fundamentals/minimal-apis) for the lightweight web server

---

<p align="center">
  Made with ❤️ for seamless screenshot control
</p>
