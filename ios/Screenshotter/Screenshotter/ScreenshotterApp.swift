import SwiftUI

@main
struct ScreenshotterApp: App {
    @StateObject private var connectionManager = ConnectionManager()
    
    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(connectionManager)
        }
    }
}
