import Foundation
import Combine

@MainActor
class ConnectionManager: ObservableObject {
    @Published var isConnected: Bool = false
    @Published var pairedIP: String? {
        didSet {
            if pairedIP != nil {
                startMonitoring()
            } else {
                stopMonitoring()
                isConnected = false
            }
        }
    }
    
    private var monitoringTask: Task<Void, Never>?
    private let checkInterval: TimeInterval = 5.0
    
    init() {}
    
    deinit {
        monitoringTask?.cancel()
    }
    
    /// Starts periodic connection monitoring
    func startMonitoring() {
        stopMonitoring()
        
        // Initial check
        Task {
            await checkConnection()
        }
        
        // Periodic checks
        monitoringTask = Task { [weak self] in
            while !Task.isCancelled {
                try? await Task.sleep(nanoseconds: UInt64(5_000_000_000)) // 5 seconds
                
                guard !Task.isCancelled else { break }
                
                await self?.checkConnection()
            }
        }
    }
    
    /// Stops connection monitoring
    func stopMonitoring() {
        monitoringTask?.cancel()
        monitoringTask = nil
    }
    
    /// Manually trigger a connection check
    func checkConnection() async {
        guard let baseURL = pairedIP else {
            isConnected = false
            return
        }
        
        do {
            let connected = try await ScreenshotService.shared.testConnection(baseURL: baseURL)
            isConnected = connected
        } catch {
            isConnected = false
        }
    }
    
    /// Force refresh connection status
    func refreshConnection() {
        Task {
            await checkConnection()
        }
    }
}
