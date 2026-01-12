import SwiftUI

struct MainView: View {
    @Binding var pairedIP: String
    @EnvironmentObject var connectionManager: ConnectionManager
    
    @State private var isCapturing = false
    @State private var lastScreenshot: UIImage?
    @State private var lastCapturedImages: [UIImage] = []
    @State private var showToast = false
    @State private var toastMessage = ""
    @State private var toastIsSuccess = true
    @State private var showScanner = false
    @State private var showShareSheet = false
    @State private var showHistory = false
    
    /// Whether there is a saved connection
    private var hasConnection: Bool {
        !pairedIP.isEmpty
    }
    
    /// Returns the display name for the selected monitor
    private var selectedMonitorName: String {
        if let selectedId = connectionManager.selectedMonitorId {
            if selectedId == "all" {
                return "All Monitors"
            }
            return connectionManager.monitors.first { $0.id == selectedId }?.name ?? "Unknown"
        }
        // Default to primary monitor
        return connectionManager.monitors.first { $0.isPrimary }?.name ?? "Primary"
    }
    
    /// Whether to show the monitor picker (more than one option available)
    private var showMonitorPicker: Bool {
        connectionManager.monitors.count > 1 || connectionManager.allowCaptureAll
    }
    
    /// Status text based on connection state
    private var statusText: String {
        if !hasConnection {
            return "No Connection Saved"
        } else if connectionManager.isConnected {
            return "Connected"
        } else {
            return "Disconnected"
        }
    }
    
    /// Status color based on connection state
    private var statusColor: Color {
        if !hasConnection {
            return .orange
        } else if connectionManager.isConnected {
            return .green
        } else {
            return .red
        }
    }
    
    var body: some View {
        NavigationStack {
            ZStack {
                VStack(spacing: 24) {
                    // Connection Status
                    HStack(spacing: 8) {
                        Circle()
                            .fill(statusColor)
                            .frame(width: 12, height: 12)
                        
                        Text(statusText)
                            .font(.subheadline)
                            .foregroundColor(.secondary)
                    }
                    .padding(.top, 20)
                    
                    // Monitor Picker (only show if multiple monitors available)
                    if showMonitorPicker && connectionManager.isConnected {
                        VStack(spacing: 8) {
                            Text("Monitor")
                                .font(.caption)
                                .foregroundColor(.secondary)
                            
                            Menu {
                                // Individual monitors
                                ForEach(connectionManager.monitors) { monitor in
                                    Button(action: {
                                        connectionManager.selectedMonitorId = monitor.id
                                    }) {
                                        HStack {
                                            Text(monitor.displayString)
                                            if connectionManager.selectedMonitorId == monitor.id ||
                                                (connectionManager.selectedMonitorId == nil && monitor.isPrimary) {
                                                Image(systemName: "checkmark")
                                            }
                                        }
                                    }
                                }
                                
                                // "All Monitors" option if allowed
                                if connectionManager.allowCaptureAll {
                                    Divider()
                                    Button(action: {
                                        connectionManager.selectedMonitorId = "all"
                                    }) {
                                        HStack {
                                            Text("All Monitors")
                                            if connectionManager.selectedMonitorId == "all" {
                                                Image(systemName: "checkmark")
                                            }
                                        }
                                    }
                                }
                            } label: {
                                HStack {
                                    Image(systemName: "display")
                                    Text(selectedMonitorName)
                                        .lineLimit(1)
                                    Image(systemName: "chevron.down")
                                        .font(.caption)
                                }
                                .padding(.horizontal, 16)
                                .padding(.vertical, 10)
                                .background(Color.gray.opacity(0.15))
                                .cornerRadius(10)
                            }
                        }
                    }
                    
                    // Scan QR Code button when no connection saved
                    if !hasConnection {
                        VStack(spacing: 16) {
                            Text("Connect to your Windows PC")
                                .font(.headline)
                                .foregroundColor(.secondary)
                            
                            Text("Scan the QR code displayed in the Screenshotter Windows app to connect.")
                                .font(.subheadline)
                                .foregroundColor(.secondary)
                                .multilineTextAlignment(.center)
                                .padding(.horizontal, 32)
                            
                            Button(action: { showScanner = true }) {
                                HStack {
                                    Image(systemName: "qrcode.viewfinder")
                                    Text("Scan QR Code")
                                }
                                .font(.headline)
                                .foregroundColor(.white)
                                .padding(.horizontal, 24)
                                .padding(.vertical, 14)
                                .background(Color.blue)
                                .cornerRadius(12)
                            }
                        }
                        .padding(.top, 40)
                    }
                    
                    Spacer()
                    
                    // Take Screenshot Button
                    Button(action: captureScreenshot) {
                        ZStack {
                            Circle()
                                .fill(
                                    LinearGradient(
                                        colors: [.blue, .blue.opacity(0.7)],
                                        startPoint: .topLeading,
                                        endPoint: .bottomTrailing
                                    )
                                )
                                .frame(width: 160, height: 160)
                                .shadow(color: .blue.opacity(0.4), radius: 15, y: 8)
                            
                            if isCapturing {
                                ProgressView()
                                    .scaleEffect(2)
                                    .tint(.white)
                            } else {
                                VStack(spacing: 8) {
                                    Image(systemName: "camera.fill")
                                        .font(.system(size: 40))
                                    Text("Capture")
                                        .font(.headline)
                                }
                                .foregroundColor(.white)
                            }
                        }
                    }
                    .disabled(isCapturing || !connectionManager.isConnected || !hasConnection)
                    .opacity(connectionManager.isConnected && hasConnection ? 1 : 0.5)
                    
                    Spacer()
                    
                    // Last Screenshot Thumbnail (tappable to view history)
                    VStack(spacing: 12) {
                        Button(action: { showHistory = true }) {
                            VStack(spacing: 8) {
                                Text("Last Screenshot")
                                    .font(.caption)
                                    .foregroundColor(.secondary)
                                
                                if let screenshot = lastScreenshot {
                                    Image(uiImage: screenshot)
                                        .resizable()
                                        .aspectRatio(contentMode: .fit)
                                        .frame(maxWidth: 200, maxHeight: 150)
                                        .cornerRadius(12)
                                        .shadow(radius: 5)
                                } else {
                                    RoundedRectangle(cornerRadius: 12)
                                        .fill(Color.gray.opacity(0.2))
                                        .frame(width: 200, height: 150)
                                        .overlay(
                                            VStack(spacing: 8) {
                                                Image(systemName: "photo")
                                                    .font(.largeTitle)
                                                    .foregroundColor(.gray)
                                                Text("No screenshot yet")
                                                    .font(.caption)
                                                    .foregroundColor(.gray)
                                            }
                                        )
                                }
                                
                                HStack(spacing: 4) {
                                    Text("View History")
                                        .font(.caption)
                                    Image(systemName: "chevron.right")
                                        .font(.caption2)
                                }
                                .foregroundColor(.blue)
                            }
                        }
                        .buttonStyle(.plain)
                        
                        // Share button (only when there's a screenshot)
                        if lastScreenshot != nil {
                            Button(action: { showShareSheet = true }) {
                                HStack(spacing: 6) {
                                    Image(systemName: "square.and.arrow.up")
                                    Text(lastCapturedImages.count > 1 ? "Share \(lastCapturedImages.count) Screenshots" : "Share")
                                }
                                .font(.subheadline)
                                .foregroundColor(.blue)
                            }
                        }
                    }
                    .padding(.bottom, 40)
                }
                
                // Toast Overlay
                if showToast {
                    VStack {
                        ToastView(message: toastMessage, isSuccess: toastIsSuccess)
                            .transition(.move(edge: .top).combined(with: .opacity))
                        Spacer()
                    }
                    .padding(.top, 50)
                    .animation(.spring(), value: showToast)
                }
            }
            .navigationTitle("Screenshotter")
            .toolbar {
                ToolbarItem(placement: .navigationBarTrailing) {
                    NavigationLink(destination: SettingsView(pairedIP: $pairedIP)) {
                        Image(systemName: "gearshape.fill")
                            .foregroundColor(.primary)
                    }
                }
            }
            .sheet(isPresented: $showScanner) {
                QRScannerView(pairedIP: $pairedIP)
            }
            .sheet(isPresented: $showShareSheet) {
                ShareSheet(items: lastCapturedImages)
            }
            .sheet(isPresented: $showHistory) {
                HistoryView()
            }
            .onChange(of: pairedIP) { _, newValue in
                // Dismiss scanner when connection is established
                if !newValue.isEmpty && showScanner {
                    showScanner = false
                }
            }
        }
    }
    
    private func captureScreenshot() {
        guard !isCapturing, connectionManager.isConnected, hasConnection else { return }
        
        isCapturing = true
        
        // Determine which monitor to capture
        let monitorId = connectionManager.selectedMonitorId
        
        Task {
            do {
                if monitorId == "all" {
                    // Capture all monitors as separate images
                    let screenshots = try await ScreenshotService.shared.captureAllMonitorsSeparately(
                        baseURL: pairedIP
                    )
                    
                    // Save each screenshot
                    for (_, image) in screenshots {
                        try await ScreenshotService.shared.saveToPhotos(image)
                    }
                    
                    await MainActor.run {
                        // Store all captured images for sharing
                        lastCapturedImages = screenshots.map { $0.image }
                        // Show the last captured image as preview
                        if let lastImage = screenshots.last?.image {
                            lastScreenshot = lastImage
                        }
                        isCapturing = false
                        showToast(message: "\(screenshots.count) screenshots saved!", isSuccess: true)
                    }
                } else {
                    // Capture single monitor
                    let image = try await ScreenshotService.shared.captureScreenshot(
                        baseURL: pairedIP,
                        monitorId: monitorId
                    )
                    try await ScreenshotService.shared.saveToPhotos(image)
                    
                    await MainActor.run {
                        lastCapturedImages = [image]
                        lastScreenshot = image
                        isCapturing = false
                        showToast(message: "Screenshot saved!", isSuccess: true)
                    }
                }
            } catch {
                await MainActor.run {
                    isCapturing = false
                    showToast(message: "Failed: \(error.localizedDescription)", isSuccess: false)
                }
            }
        }
    }
    
    private func showToast(message: String, isSuccess: Bool) {
        toastMessage = message
        toastIsSuccess = isSuccess
        showToast = true
        
        DispatchQueue.main.asyncAfter(deadline: .now() + 3) {
            withAnimation {
                showToast = false
            }
        }
    }
}

// MARK: - Share Sheet

struct ShareSheet: UIViewControllerRepresentable {
    let items: [Any]
    
    func makeUIViewController(context: Context) -> UIActivityViewController {
        UIActivityViewController(activityItems: items, applicationActivities: nil)
    }
    
    func updateUIViewController(_ uiViewController: UIActivityViewController, context: Context) {}
}

#Preview {
    MainView(pairedIP: .constant("http://192.168.1.100:5000"))
        .environmentObject(ConnectionManager())
}
