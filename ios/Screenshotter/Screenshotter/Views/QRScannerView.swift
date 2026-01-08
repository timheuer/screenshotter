import SwiftUI
@preconcurrency import AVFoundation

struct QRScannerView: View {
    @Binding var pairedIP: String
    @State private var isScanning = true
    @State private var showError = false
    @State private var errorMessage = ""
    @State private var isValidating = false
    @State private var scannerID = UUID()
    
    var body: some View {
        NavigationStack {
            ZStack {
                if isScanning {
                    QRCodeScannerRepresentable(
                        onCodeScanned: handleScannedCode
                    )
                    .id(scannerID)
                    .ignoresSafeArea()
                    
                    VStack {
                        Spacer()
                        
                        RoundedRectangle(cornerRadius: 12)
                            .stroke(Color.white, lineWidth: 3)
                            .frame(width: 250, height: 250)
                            .background(Color.clear)
                        
                        Spacer()
                        
                        Text("Point camera at QR code on Windows PC")
                            .font(.headline)
                            .foregroundColor(.white)
                            .padding()
                            .background(Color.black.opacity(0.7))
                            .cornerRadius(10)
                            .padding(.bottom, 50)
                    }
                    
                    if isValidating {
                        Color.black.opacity(0.5)
                            .ignoresSafeArea()
                        
                        VStack(spacing: 16) {
                            ProgressView()
                                .scaleEffect(1.5)
                                .tint(.white)
                            Text("Validating connection...")
                                .foregroundColor(.white)
                                .font(.headline)
                        }
                        .padding(30)
                        .background(Color.black.opacity(0.8))
                        .cornerRadius(16)
                    }
                }
            }
            .navigationTitle("Scan QR Code")
            .navigationBarTitleDisplayMode(.inline)
            .alert("Connection Error", isPresented: $showError) {
                Button("Try Again") {
                    scannerID = UUID()
                    isScanning = true
                }
            } message: {
                Text(errorMessage)
            }
        }
    }
    
    private func handleScannedCode(_ code: String) {
        guard !isValidating else { return }
        isValidating = true
        
        Task {
            do {
                guard let data = code.data(using: .utf8),
                      let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
                      let ip = json["ip"] as? String,
                      let port = json["port"] as? Int else {
                    throw ScanError.invalidQRCode
                }
                
                let baseURL = "http://\(ip):\(port)"
                let isValid = try await ScreenshotService.shared.testConnection(baseURL: baseURL)
                
                if isValid {
                    await MainActor.run {
                        pairedIP = baseURL
                        isValidating = false
                    }
                } else {
                    throw ScanError.connectionFailed
                }
            } catch {
                await MainActor.run {
                    isValidating = false
                    errorMessage = error.localizedDescription
                    showError = true
                    isScanning = true
                }
            }
        }
    }
}

enum ScanError: LocalizedError {
    case invalidQRCode
    case connectionFailed
    
    var errorDescription: String? {
        switch self {
        case .invalidQRCode:
            return "Invalid QR code format. Please scan a valid Screenshotter QR code."
        case .connectionFailed:
            return "Could not connect to the Windows PC. Make sure it's running and on the same network."
        }
    }
}

// MARK: - QR Code Scanner UIKit Wrapper

struct QRCodeScannerRepresentable: UIViewControllerRepresentable {
    let onCodeScanned: (String) -> Void
    
    func makeUIViewController(context: Context) -> QRCodeScannerViewController {
        let controller = QRCodeScannerViewController()
        controller.onCodeScanned = onCodeScanned
        return controller
    }
    
    func updateUIViewController(_ uiViewController: QRCodeScannerViewController, context: Context) {}
}

class QRCodeScannerViewController: UIViewController, AVCaptureMetadataOutputObjectsDelegate {
    var captureSession: AVCaptureSession?
    var previewLayer: AVCaptureVideoPreviewLayer?
    var onCodeScanned: ((String) -> Void)?
    private var hasScanned = false
    
    override func viewDidLoad() {
        super.viewDidLoad()
        setupCamera()
    }
    
    override func viewWillAppear(_ animated: Bool) {
        super.viewWillAppear(animated)
        hasScanned = false
        let session = captureSession
        if session?.isRunning == false {
            DispatchQueue.global(qos: .userInitiated).async {
                session?.startRunning()
            }
        }
    }
    
    override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        if captureSession?.isRunning == true {
            captureSession?.stopRunning()
        }
    }
    
    override func viewDidLayoutSubviews() {
        super.viewDidLayoutSubviews()
        previewLayer?.frame = view.layer.bounds
    }
    
    private func setupCamera() {
        captureSession = AVCaptureSession()
        
        guard let videoCaptureDevice = AVCaptureDevice.default(for: .video),
              let captureSession = captureSession else {
            return
        }
        
        do {
            let videoInput = try AVCaptureDeviceInput(device: videoCaptureDevice)
            
            if captureSession.canAddInput(videoInput) {
                captureSession.addInput(videoInput)
            } else {
                return
            }
            
            let metadataOutput = AVCaptureMetadataOutput()
            
            if captureSession.canAddOutput(metadataOutput) {
                captureSession.addOutput(metadataOutput)
                metadataOutput.setMetadataObjectsDelegate(self, queue: DispatchQueue.main)
                metadataOutput.metadataObjectTypes = [.qr]
            } else {
                return
            }
            
            previewLayer = AVCaptureVideoPreviewLayer(session: captureSession)
            previewLayer?.frame = view.layer.bounds
            previewLayer?.videoGravity = .resizeAspectFill
            
            if let previewLayer = previewLayer {
                view.layer.addSublayer(previewLayer)
            }
            
            let session = captureSession
            DispatchQueue.global(qos: .userInitiated).async {
                session.startRunning()
            }
        } catch {
            return
        }
    }
    
    nonisolated func metadataOutput(_ output: AVCaptureMetadataOutput, didOutput metadataObjects: [AVMetadataObject], from connection: AVCaptureConnection) {
        guard let metadataObject = metadataObjects.first as? AVMetadataMachineReadableCodeObject,
              metadataObject.type == .qr,
              let stringValue = metadataObject.stringValue else {
            return
        }
        
        Task { @MainActor in
            guard !hasScanned else { return }
            hasScanned = true
            captureSession?.stopRunning()
            
            AudioServicesPlaySystemSound(SystemSoundID(kSystemSoundID_Vibrate))
            onCodeScanned?(stringValue)
        }
    }
}

#Preview {
    QRScannerView(pairedIP: .constant(""))
}
