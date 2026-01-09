import Foundation
import Combine

@MainActor
class ConnectionManager: ObservableObject {
    @Published var isConnected: Bool = false
    @Published var monitors: [MonitorInfo] = []
    @Published var allowCaptureAll: Bool = true
    @Published var selectedMonitorId: String? = nil // nil = primary monitor
    
    @Published var pairedIP: String? {
        didSet {
            if pairedIP != nil {
                startMonitoring()
            } else {
                stopMonitoring()
                isConnected = false
                monitors = []
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
            monitors = []
            return
        }
        
        do {
            let connected = try await ScreenshotService.shared.testConnection(baseURL: baseURL)
            isConnected = connected
            
            if connected {
                // Fetch available monitors
                await fetchMonitors()
            } else {
                monitors = []
            }
        } catch {
            isConnected = false
            monitors = []
        }
    }
    
    /// Fetches available monitors from the server
    func fetchMonitors() async {
        guard let baseURL = pairedIP else { return }
        
        do {
            let response = try await ScreenshotService.shared.fetchMonitors(baseURL: baseURL)
            monitors = response.monitors
            allowCaptureAll = response.allowCaptureAll
            
            // Reset selection if current selection is no longer valid
            if let selectedId = selectedMonitorId,
               selectedId != "all",
               !monitors.contains(where: { $0.id == selectedId }) {
                selectedMonitorId = nil
            }
        } catch {
            // Keep existing monitors on error
            print("Failed to fetch monitors: \(error)")
        }
    }
    
    /// Force refresh connection status
    func refreshConnection() {
        Task {
            await checkConnection()
        }
    }
}
