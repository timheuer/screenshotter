import UIKit
import Photos
import ImageIO

actor ScreenshotService {
    static let shared = ScreenshotService()
    
    private init() {}
    
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
    
    /// Saves an image to the Photos library with metadata identifying it as a remote screenshot
    /// - Parameter image: The image to save
    func saveToPhotos(_ image: UIImage) async throws {
        let status = await PHPhotoLibrary.requestAuthorization(for: .addOnly)
        
        guard status == .authorized || status == .limited else {
            throw ScreenshotError.photoLibraryAccessDenied
        }
        
        // Embed metadata into the image
        let imageDataWithMetadata = embedMetadata(in: image)
        
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
