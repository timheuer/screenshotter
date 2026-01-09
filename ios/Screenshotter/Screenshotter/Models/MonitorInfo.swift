import Foundation

/// Represents a monitor/display on the Windows PC
struct MonitorInfo: Codable, Identifiable, Equatable {
    let id: String
    let name: String
    let width: Int
    let height: Int
    let x: Int
    let y: Int
    let isPrimary: Bool
    
    /// Display string showing name and resolution
    var displayString: String {
        "\(name) (\(width)×\(height))"
    }
}

/// Response from /api/monitors endpoint
struct MonitorsResponse: Codable {
    let monitors: [MonitorInfo]
    let allowCaptureAll: Bool
}
