import SwiftUI

struct MainView: View {
    @Binding var pairedIP: String
    @EnvironmentObject var connectionManager: ConnectionManager
    
    @State private var isCapturing = false
    @State private var lastScreenshot: UIImage?
    @State private var showToast = false
    @State private var toastMessage = ""
    @State private var toastIsSuccess = true
    
    var body: some View {
        NavigationStack {
            ZStack {
                VStack(spacing: 30) {
                    // Connection Status
                    HStack(spacing: 8) {
                        Circle()
                            .fill(connectionManager.isConnected ? Color.green : Color.red)
                            .frame(width: 12, height: 12)
                        
                        Text(connectionManager.isConnected ? "Connected" : "Disconnected")
                            .font(.subheadline)
                            .foregroundColor(.secondary)
                    }
                    .padding(.top, 20)
                    
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
                    .disabled(isCapturing || !connectionManager.isConnected)
                    .opacity(connectionManager.isConnected ? 1 : 0.5)
                    
                    Spacer()
                    
                    // Last Screenshot Thumbnail
                    VStack(spacing: 12) {
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
        }
    }
    
    private func captureScreenshot() {
        guard !isCapturing, connectionManager.isConnected else { return }
        
        isCapturing = true
        
        Task {
            do {
                let image = try await ScreenshotService.shared.captureScreenshot(baseURL: pairedIP)
                try await ScreenshotService.shared.saveToPhotos(image)
                
                await MainActor.run {
                    lastScreenshot = image
                    isCapturing = false
                    showToast(message: "Screenshot saved!", isSuccess: true)
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

#Preview {
    MainView(pairedIP: .constant("http://192.168.1.100:5000"))
        .environmentObject(ConnectionManager())
}
