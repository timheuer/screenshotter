import SwiftUI

struct SettingsView: View {
    @Binding var pairedIP: String
    @Environment(\.dismiss) private var dismiss
    @EnvironmentObject var connectionManager: ConnectionManager
    
    @State private var manualIP: String = ""
    @State private var manualPort: String = "5000"
    @State private var isTesting = false
    @State private var showTestResult = false
    @State private var testResultSuccess = false
    @State private var showRescanConfirmation = false
    @State private var showClearConfirmation = false
    
    private var appVersion: String {
        let version = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "1.0"
        let build = Bundle.main.infoDictionary?["CFBundleVersion"] as? String ?? "1"
        return "\(version) (\(build))"
    }
    
    var body: some View {
        Form {
            Section {
                HStack {
                    Text("Status")
                    Spacer()
                    HStack(spacing: 6) {
                        Circle()
                            .fill(pairedIP.isEmpty ? Color.orange : (connectionManager.isConnected ? Color.green : Color.red))
                            .frame(width: 10, height: 10)
                        Text(pairedIP.isEmpty ? "No Connection Saved" : (connectionManager.isConnected ? "Connected" : "Disconnected"))
                            .foregroundColor(.secondary)
                    }
                }
                
                if !pairedIP.isEmpty {
                    HStack {
                        Text("Server")
                        Spacer()
                        Text(pairedIP)
                            .foregroundColor(.secondary)
                            .lineLimit(1)
                            .truncationMode(.middle)
                    }
                    
                    Button(role: .destructive, action: {
                        showClearConfirmation = true
                    }) {
                        HStack {
                            Image(systemName: "trash")
                            Text("Clear Connection")
                        }
                    }
                    .confirmationDialog(
                        "Clear Connection?",
                        isPresented: $showClearConfirmation,
                        titleVisibility: .visible
                    ) {
                        Button("Clear", role: .destructive) {
                            pairedIP = ""
                            manualIP = ""
                            manualPort = "5000"
                            showTestResult = false
                        }
                        Button("Cancel", role: .cancel) {}
                    } message: {
                        Text("This will remove the saved connection information. You can scan or enter a new connection later.")
                    }
                }
            } header: {
                Text("Current Connection")
            }
            
            if !pairedIP.isEmpty {
                Section {
                    Button(action: {
                        showRescanConfirmation = true
                    }) {
                        HStack {
                            Image(systemName: "qrcode.viewfinder")
                            Text("Re-scan QR Code")
                        }
                    }
                    .confirmationDialog(
                        "Re-scan QR Code?",
                        isPresented: $showRescanConfirmation,
                        titleVisibility: .visible
                    ) {
                        Button("Re-scan", role: .destructive) {
                            pairedIP = ""
                            dismiss()
                        }
                        Button("Cancel", role: .cancel) {}
                    } message: {
                        Text("This will disconnect from the current PC and open the QR scanner.")
                    }
                } header: {
                    Text("Pairing")
                }
            }
            
            Section {
                TextField("IP Address", text: $manualIP)
                    .keyboardType(.decimalPad)
                    .autocapitalization(.none)
                    .disableAutocorrection(true)
                
                TextField("Port", text: $manualPort)
                    .keyboardType(.numberPad)
                
                Button(action: testManualConnection) {
                    HStack {
                        if isTesting {
                            ProgressView()
                                .scaleEffect(0.8)
                        } else {
                            Image(systemName: "network")
                        }
                        Text("Test Connection")
                    }
                }
                .disabled(manualIP.isEmpty || isTesting)
                
                if showTestResult {
                    HStack {
                        Image(systemName: testResultSuccess ? "checkmark.circle.fill" : "xmark.circle.fill")
                            .foregroundColor(testResultSuccess ? .green : .red)
                        Text(testResultSuccess ? "Connection successful!" : "Connection failed")
                            .foregroundColor(testResultSuccess ? .green : .red)
                    }
                }
                
                if showTestResult && testResultSuccess {
                    Button("Use This Connection") {
                        pairedIP = "http://\(manualIP):\(manualPort)"
                        dismiss()
                    }
                    .foregroundColor(.blue)
                }
            } header: {
                Text("Manual Connection")
            } footer: {
                Text("Enter the IP address and port shown on your Windows PC.")
            }
            
            Section {
                HStack {
                    Text("Version")
                    Spacer()
                    Text(appVersion)
                        .foregroundColor(.secondary)
                }
            } header: {
                Text("About")
            } footer: {
                Text("Screenshotter for iOS\nCapture screenshots from your Windows PC.")
            }
        }
        .navigationTitle("Settings")
        .navigationBarTitleDisplayMode(.inline)
        .onAppear {
            if !pairedIP.isEmpty {
                parseExistingIP()
            }
        }
    }
    
    private func parseExistingIP() {
        // Parse http://ip:port format
        let urlString = pairedIP.replacingOccurrences(of: "http://", with: "")
        let components = urlString.split(separator: ":")
        if components.count >= 1 {
            manualIP = String(components[0])
        }
        if components.count >= 2 {
            manualPort = String(components[1])
        }
    }
    
    private func testManualConnection() {
        guard !manualIP.isEmpty else { return }
        
        isTesting = true
        showTestResult = false
        
        let baseURL = "http://\(manualIP):\(manualPort)"
        
        Task {
            do {
                let success = try await ScreenshotService.shared.testConnection(baseURL: baseURL)
                await MainActor.run {
                    testResultSuccess = success
                    showTestResult = true
                    isTesting = false
                }
            } catch {
                await MainActor.run {
                    testResultSuccess = false
                    showTestResult = true
                    isTesting = false
                }
            }
        }
    }
}

#Preview {
    NavigationStack {
        SettingsView(pairedIP: .constant("http://192.168.1.100:5000"))
            .environmentObject(ConnectionManager())
    }
}
