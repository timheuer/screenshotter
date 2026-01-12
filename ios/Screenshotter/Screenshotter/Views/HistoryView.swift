import SwiftUI
import Photos

struct HistoryView: View {
    @Environment(\.dismiss) private var dismiss
    
    @State private var assets: [PHAsset] = []
    @State private var isLoading = true
    @State private var selectedAsset: PHAsset?
    @State private var imageToShare: UIImage?
    @State private var isLoadingImage = false
    
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
            .sheet(item: $selectedAsset) { asset in
                ImageShareView(asset: asset)
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
                        selectedAsset = asset
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
}

// MARK: - Image Share View

struct ImageShareView: View {
    let asset: PHAsset
    @Environment(\.dismiss) private var dismiss
    
    @State private var image: UIImage?
    @State private var isLoading = true
    @State private var showShareSheet = false
    
    var body: some View {
        NavigationStack {
            Group {
                if isLoading {
                    ProgressView("Loading image...")
                } else if let image = image {
                    VStack(spacing: 20) {
                        Image(uiImage: image)
                            .resizable()
                            .aspectRatio(contentMode: .fit)
                            .cornerRadius(12)
                            .padding()
                        
                        Button(action: { showShareSheet = true }) {
                            HStack {
                                Image(systemName: "square.and.arrow.up")
                                Text("Share")
                            }
                            .font(.headline)
                            .foregroundColor(.white)
                            .padding(.horizontal, 32)
                            .padding(.vertical, 14)
                            .background(Color.blue)
                            .cornerRadius(12)
                        }
                    }
                } else {
                    VStack(spacing: 16) {
                        Image(systemName: "exclamationmark.triangle")
                            .font(.system(size: 40))
                            .foregroundColor(.orange)
                        Text("Could not load image")
                            .foregroundColor(.secondary)
                    }
                }
            }
            .navigationTitle("Screenshot")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button("Done") {
                        dismiss()
                    }
                }
            }
            .sheet(isPresented: $showShareSheet) {
                if let image = image {
                    ShareSheet(items: [image])
                }
            }
        }
        .task {
            await loadImage()
        }
    }
    
    private func loadImage() async {
        isLoading = true
        image = await ScreenshotService.shared.loadFullImage(from: asset)
        isLoading = false
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

// MARK: - PHAsset Identifiable Extension

extension PHAsset: @retroactive Identifiable {
    public var id: String { localIdentifier }
}

#Preview {
    HistoryView()
}
