import UIKit
import Photos
import ImageIO

actor ScreenshotService {
    static let shared = ScreenshotService()
    
    private init() {}
    
    /// Album name for storing screenshots (static so it can be accessed from nonisolated contexts)
    private static let albumName = "Screenshotter"
    
    /// App bundle identifier for metadata
    private var appBundleId: String {
        Bundle.main.bundleIdentifier ?? "com.timheuer.screenshotter"
    }
    
    /// App name for metadata
    private var appName: String {
        Bundle.main.object(forInfoDictionaryKey: "CFBundleName") as? String ?? "Screenshotter"
    }
    
    enum ScreenshotError: LocalizedError {
        case invalidURL
        case networkError(Error)
        case invalidResponse
        case serverError(Int)
        case invalidImageData
        case photoLibraryAccessDenied
        case saveFailed(Error)
        case monitorNotAllowed
        
        var errorDescription: String? {
            switch self {
            case .invalidURL:
                return "Invalid server URL"
            case .networkError(let error):
                return "Network error: \(error.localizedDescription)"
            case .invalidResponse:
                return "Invalid response from server"
            case .serverError(let code):
                return "Server error (code: \(code))"
            case .invalidImageData:
                return "Could not decode screenshot image"
            case .photoLibraryAccessDenied:
                return "Photo library access denied"
            case .saveFailed(let error):
                return "Failed to save: \(error.localizedDescription)"
            case .monitorNotAllowed:
                return "This monitor is not allowed for capture"
            }
        }
    }
    
    /// Fetches available monitors from the Windows PC
    /// - Parameter baseURL: The base URL of the screenshot server
    /// - Returns: MonitorsResponse containing available monitors and capture settings
    func fetchMonitors(baseURL: String) async throws -> MonitorsResponse {
        guard let url = URL(string: "\(baseURL)/api/monitors") else {
            throw ScreenshotError.invalidURL
        }
        
        var request = URLRequest(url: url)
        request.httpMethod = "GET"
        request.timeoutInterval = 10
        
        let data: Data
        let response: URLResponse
        
        do {
            (data, response) = try await URLSession.shared.data(for: request)
        } catch {
            throw ScreenshotError.networkError(error)
        }
        
        guard let httpResponse = response as? HTTPURLResponse else {
            throw ScreenshotError.invalidResponse
        }
        
        guard httpResponse.statusCode == 200 else {
            throw ScreenshotError.serverError(httpResponse.statusCode)
        }
        
        let decoder = JSONDecoder()
        return try decoder.decode(MonitorsResponse.self, from: data)
    }
    
    /// Captures a screenshot from the Windows PC
    /// - Parameters:
    ///   - baseURL: The base URL of the screenshot server (e.g., "http://192.168.1.100:5000")
    ///   - monitorId: Optional monitor ID to capture. nil = primary, "all" = all screens
    /// - Returns: The captured screenshot as UIImage
    func captureScreenshot(baseURL: String, monitorId: String? = nil) async throws -> UIImage {
        var urlString = "\(baseURL)/api/screenshot"
        if let monitorId = monitorId {
            urlString += "?monitor=\(monitorId)"
        }
        
        guard let url = URL(string: urlString) else {
            throw ScreenshotError.invalidURL
        }
        
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.timeoutInterval = 30
        
        let data: Data
        let response: URLResponse
        
        do {
            (data, response) = try await URLSession.shared.data(for: request)
        } catch {
            throw ScreenshotError.networkError(error)
        }
        
        guard let httpResponse = response as? HTTPURLResponse else {
            throw ScreenshotError.invalidResponse
        }
        
        guard httpResponse.statusCode == 200 else {
            throw ScreenshotError.serverError(httpResponse.statusCode)
        }
        
        guard let image = UIImage(data: data) else {
            throw ScreenshotError.invalidImageData
        }
        
        return image
    }
    
    /// Captures all monitors as separate images from the Windows PC
    /// - Parameter baseURL: The base URL of the screenshot server
    /// - Returns: Array of tuples containing monitor name and captured image
    func captureAllMonitorsSeparately(baseURL: String) async throws -> [(name: String, image: UIImage)] {
        guard let url = URL(string: "\(baseURL)/api/screenshot/all-separate") else {
            throw ScreenshotError.invalidURL
        }
        
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.timeoutInterval = 60 // Allow more time for multiple captures
        
        let data: Data
        let response: URLResponse
        
        do {
            (data, response) = try await URLSession.shared.data(for: request)
        } catch {
            throw ScreenshotError.networkError(error)
        }
        
        guard let httpResponse = response as? HTTPURLResponse else {
            throw ScreenshotError.invalidResponse
        }
        
        guard httpResponse.statusCode == 200 else {
            throw ScreenshotError.serverError(httpResponse.statusCode)
        }
        
        let decoder = JSONDecoder()
        let screenshots = try decoder.decode([SeparateScreenshotResponse].self, from: data)
        
        var results: [(name: String, image: UIImage)] = []
        
        for screenshot in screenshots {
            guard let imageData = Data(base64Encoded: screenshot.imageBase64),
                  let image = UIImage(data: imageData) else {
                continue // Skip invalid images
            }
            results.append((name: screenshot.monitorName, image: image))
        }
        
        if results.isEmpty {
            throw ScreenshotError.invalidImageData
        }
        
        return results
    }
    
    /// Saves an image to the Photos library with metadata identifying it as a remote screenshot
    /// - Parameter image: The image to save
    func saveToPhotos(_ image: UIImage) async throws {
        // Request readWrite to enable album creation/access for history feature
        let status = await PHPhotoLibrary.requestAuthorization(for: .readWrite)
        
        guard status == .authorized || status == .limited else {
            throw ScreenshotError.photoLibraryAccessDenied
        }
        
        // Embed metadata into the image
        let imageDataWithMetadata = embedMetadata(in: image)
        
        // Try to get or create the album (don't fail if this doesn't work)
        var album: PHAssetCollection? = nil
        do {
            album = try await getOrCreateAlbum()
        } catch {
            // Album creation failed, but we can still save the photo
            album = nil
        }
        
        try await withCheckedThrowingContinuation { (continuation: CheckedContinuation<Void, Error>) in
            PHPhotoLibrary.shared().performChanges {
                let request = PHAssetCreationRequest.forAsset()
                request.creationDate = Date()
                
                if let imageData = imageDataWithMetadata {
                    request.addResource(with: .photo, data: imageData, options: nil)
                } else if let fallbackData = image.pngData() {
                    // Fallback to PNG without metadata if embedding fails
                    request.addResource(with: .photo, data: fallbackData, options: nil)
                }
                
                // Add to album if available
                if let album = album,
                   let placeholder = request.placeholderForCreatedAsset {
                    let albumChangeRequest = PHAssetCollectionChangeRequest(for: album)
                    albumChangeRequest?.addAssets([placeholder] as NSArray)
                }
            } completionHandler: { success, error in
                if success {
                    continuation.resume()
                } else if let error = error {
                    continuation.resume(throwing: ScreenshotError.saveFailed(error))
                } else {
                    continuation.resume(throwing: ScreenshotError.saveFailed(NSError(domain: "ScreenshotService", code: -1)))
                }
            }
        }
    }
    
    /// Gets the Screenshotter album or creates it if it doesn't exist
    /// - Returns: The album, or nil if creation fails
    private func getOrCreateAlbum() async throws -> PHAssetCollection? {
        // Capture album name for use in closures
        let targetAlbumName = Self.albumName
        
        // First, try to find existing album
        let fetchOptions = PHFetchOptions()
        fetchOptions.predicate = NSPredicate(format: "title = %@", targetAlbumName)
        let collections = PHAssetCollection.fetchAssetCollections(with: .album, subtype: .any, options: fetchOptions)
        
        if let existingAlbum = collections.firstObject {
            return existingAlbum
        }
        
        // Create new album
        var albumPlaceholder: PHObjectPlaceholder?
        
        do {
            try await withCheckedThrowingContinuation { (continuation: CheckedContinuation<Void, Error>) in
                PHPhotoLibrary.shared().performChanges {
                    let createRequest = PHAssetCollectionChangeRequest.creationRequestForAssetCollection(withTitle: targetAlbumName)
                    albumPlaceholder = createRequest.placeholderForCreatedAssetCollection
                } completionHandler: { success, error in
                    if success {
                        continuation.resume()
                    } else if let error = error {
                        continuation.resume(throwing: error)
                    } else {
                        continuation.resume()
                    }
                }
            }
        } catch {
            // If album creation fails, return nil but don't crash - photo will still be saved
            return nil
        }
        
        // Fetch the newly created album
        guard let placeholder = albumPlaceholder else { return nil }
        let newAlbumFetch = PHAssetCollection.fetchAssetCollections(withLocalIdentifiers: [placeholder.localIdentifier], options: nil)
        return newAlbumFetch.firstObject
    }
    
    /// Fetches recent screenshots from the Screenshotter album
    /// - Parameter limit: Maximum number of screenshots to fetch
    /// - Returns: Array of PHAsset objects, newest first
    nonisolated func fetchRecentScreenshots(limit: Int = 20) async -> [PHAsset] {
        // Check for read access
        let status = await PHPhotoLibrary.requestAuthorization(for: .readWrite)
        guard status == .authorized || status == .limited else {
            return []
        }
        
        // Find the album
        let fetchOptions = PHFetchOptions()
        fetchOptions.predicate = NSPredicate(format: "title = %@", Self.albumName)
        let collections = PHAssetCollection.fetchAssetCollections(with: .album, subtype: .any, options: fetchOptions)
        
        guard let album = collections.firstObject else {
            return []
        }
        
        // Fetch assets from album
        let assetFetchOptions = PHFetchOptions()
        assetFetchOptions.sortDescriptors = [NSSortDescriptor(key: "creationDate", ascending: false)]
        assetFetchOptions.fetchLimit = limit
        
        let assets = PHAsset.fetchAssets(in: album, options: assetFetchOptions)
        
        var result: [PHAsset] = []
        assets.enumerateObjects { asset, _, _ in
            result.append(asset)
        }
        
        return result
    }
    
    /// Loads an image from a PHAsset
    /// - Parameters:
    ///   - asset: The asset to load
    ///   - targetSize: Target size for the image
    /// - Returns: The loaded UIImage, or nil if loading fails
    nonisolated func loadImage(from asset: PHAsset, targetSize: CGSize) async -> UIImage? {
        await withCheckedContinuation { continuation in
            let options = PHImageRequestOptions()
            options.deliveryMode = .highQualityFormat
            options.isNetworkAccessAllowed = true
            options.isSynchronous = false
            
            PHImageManager.default().requestImage(
                for: asset,
                targetSize: targetSize,
                contentMode: .aspectFit,
                options: options
            ) { image, _ in
                continuation.resume(returning: image)
            }
        }
    }
    
    /// Loads a full-resolution image from a PHAsset for sharing
    /// - Parameter asset: The asset to load
    /// - Returns: The loaded UIImage, or nil if loading fails
    nonisolated func loadFullImage(from asset: PHAsset) async -> UIImage? {
        await withCheckedContinuation { continuation in
            let options = PHImageRequestOptions()
            options.deliveryMode = .highQualityFormat
            options.isNetworkAccessAllowed = true
            options.isSynchronous = false
            
            PHImageManager.default().requestImage(
                for: asset,
                targetSize: PHImageManagerMaximumSize,
                contentMode: .aspectFit,
                options: options
            ) { image, _ in
                continuation.resume(returning: image)
            }
        }
    }
    
    /// Embeds metadata into the image identifying it as a remote screenshot from this app
    /// - Parameter image: The source image
    /// - Returns: JPEG data with embedded EXIF/TIFF metadata, or nil if embedding fails
    private func embedMetadata(in image: UIImage) -> Data? {
        guard let cgImage = image.cgImage else { return nil }
        
        let mutableData = NSMutableData()
        guard let destination = CGImageDestinationCreateWithData(
            mutableData,
            "public.jpeg" as CFString,
            1,
            nil
        ) else { return nil }
        
        // Build metadata dictionary
        let tiffMetadata: [String: Any] = [
            kCGImagePropertyTIFFSoftware as String: "\(appName) (\(appBundleId))",
            kCGImagePropertyTIFFArtist as String: appName,
            kCGImagePropertyTIFFImageDescription as String: "Remote screenshot captured via \(appName)"
        ]
        
        let exifMetadata: [String: Any] = [
            kCGImagePropertyExifUserComment as String: "Remote screenshot captured from Windows PC via \(appName)",
            kCGImagePropertyExifDateTimeOriginal as String: exifDateString(from: Date()),
            kCGImagePropertyExifDateTimeDigitized as String: exifDateString(from: Date())
        ]
        
        let metadata: [String: Any] = [
            kCGImagePropertyTIFFDictionary as String: tiffMetadata,
            kCGImagePropertyExifDictionary as String: exifMetadata
        ]
        
        CGImageDestinationAddImage(destination, cgImage, metadata as CFDictionary)
        
        guard CGImageDestinationFinalize(destination) else { return nil }
        
        return mutableData as Data
    }
    
    /// Formats a date as an EXIF-compatible string
    /// - Parameter date: The date to format
    /// - Returns: EXIF date string in "yyyy:MM:dd HH:mm:ss" format
    private func exifDateString(from date: Date) -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "yyyy:MM:dd HH:mm:ss"
        formatter.locale = Locale(identifier: "en_US_POSIX")
        return formatter.string(from: date)
    }
    
    /// Tests the connection to the screenshot server
    /// - Parameter baseURL: The base URL of the screenshot server
    /// - Returns: True if connection is successful
    func testConnection(baseURL: String) async throws -> Bool {
        guard let url = URL(string: "\(baseURL)/api/info") else {
            throw ScreenshotError.invalidURL
        }
        
        var request = URLRequest(url: url)
        request.httpMethod = "GET"
        request.timeoutInterval = 10
        
        do {
            let (_, response) = try await URLSession.shared.data(for: request)
            
            guard let httpResponse = response as? HTTPURLResponse else {
                return false
            }
            
            return httpResponse.statusCode == 200
        } catch {
            throw ScreenshotError.networkError(error)
        }
    }
}
