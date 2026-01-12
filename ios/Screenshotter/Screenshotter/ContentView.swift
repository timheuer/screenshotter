import SwiftUI

struct ContentView: View {
    @AppStorage("pairedIP") private var pairedIP: String = ""
    @EnvironmentObject var connectionManager: ConnectionManager
    
    var body: some View {
        MainView(pairedIP: $pairedIP)
            .onChange(of: pairedIP) { _, newValue in
                connectionManager.pairedIP = newValue.isEmpty ? nil : newValue
            }
            .onAppear {
                connectionManager.pairedIP = pairedIP.isEmpty ? nil : pairedIP
            }
    }
}

#Preview {
    ContentView()
        .environmentObject(ConnectionManager())
}
