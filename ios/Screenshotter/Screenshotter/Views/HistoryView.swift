import SwiftUI
import Photos

struct HistoryView: View {
    @Environment(\.dismiss) private var dismiss
    
    @State private var assets: [PHAsset] = []
    @State private var isLoading = true
    @State private var selectedAsset: PHAsset?
    @State private var selectedImage: UIImage?
    @State private var showShareSheet = false
    
    private let columns = [
        GridItem(.flexible(), spacing: 8),
        GridItem(.flexible(), spacing: 8),
        GridItem(.flexible(), spacing: 8)
    ]
    
    var body: some View {
        NavigationStack {
            Group {
                if isLoading {
                    ProgressView("Loading...")
                } else if assets.isEmpty {
                    emptyStateView
                } else {
                    scrollableGrid
                }
            }
            .navigationTitle("Screenshot History")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button("Done") {
                        dismiss()
                    }
                }
            }
            .sheet(isPresented: $showShareSheet) {
                if let image = selectedImage {
                    ShareSheet(items: [image])
                }
            }
        }
        .task {
            await loadScreenshots()
        }
    }
    
    private var emptyStateView: some View {
        VStack(spacing: 16) {
            Image(systemName: "photo.on.rectangle.angled")
                .font(.system(size: 60))
                .foregroundColor(.gray)
            
            Text("No Screenshots Yet")
                .font(.headline)
                .foregroundColor(.secondary)
            
            Text("Screenshots you capture will appear here.")
                .font(.subheadline)
                .foregroundColor(.secondary)
                .multilineTextAlignment(.center)
                .padding(.horizontal, 32)
        }
    }
    
    private var scrollableGrid: some View {
        ScrollView {
            LazyVGrid(columns: columns, spacing: 8) {
                ForEach(assets, id: \.localIdentifier) { asset in
                    ScreenshotThumbnail(asset: asset) {
                        selectAndShare(asset: asset)
                    }
                }
            }
            .padding()
        }
    }
    
    private func loadScreenshots() async {
        isLoading = true
        assets = await ScreenshotService.shared.fetchRecentScreenshots(limit: 50)
        isLoading = false
    }
    
    private func selectAndShare(asset: PHAsset) {
        selectedAsset = asset
        
        Task {
            if let image = await ScreenshotService.shared.loadFullImage(from: asset) {
                await MainActor.run {
                    selectedImage = image
                    showShareSheet = true
                }
            }
        }
    }
}

// MARK: - Screenshot Thumbnail

struct ScreenshotThumbnail: View {
    let asset: PHAsset
    let onTap: () -> Void
    
    @State private var thumbnail: UIImage?
    
    var body: some View {
        Button(action: onTap) {
            ZStack {
                if let thumbnail = thumbnail {
                    Image(uiImage: thumbnail)
                        .resizable()
                        .aspectRatio(contentMode: .fill)
                        .frame(minWidth: 0, maxWidth: .infinity)
                        .aspectRatio(1, contentMode: .fit)
                        .clipped()
                        .cornerRadius(8)
                } else {
                    RoundedRectangle(cornerRadius: 8)
                        .fill(Color.gray.opacity(0.2))
                        .aspectRatio(1, contentMode: .fit)
                        .overlay(
                            ProgressView()
                                .scaleEffect(0.8)
                        )
                }
            }
        }
        .buttonStyle(.plain)
        .task {
            await loadThumbnail()
        }
    }
    
    private func loadThumbnail() async {
        let size = CGSize(width: 200, height: 200)
        if let image = await ScreenshotService.shared.loadImage(from: asset, targetSize: size) {
            thumbnail = image
        }
    }
}

#Preview {
    HistoryView()
}
