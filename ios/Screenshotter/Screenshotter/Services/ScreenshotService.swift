import UIKit
import Photos

actor ScreenshotService {
    static let shared = ScreenshotService()
    
    private init() {}
    
    enum ScreenshotError: LocalizedError {
        case invalidURL
        case networkError(Error)
        case invalidResponse
        case serverError(Int)
        case invalidImageData
        case photoLibraryAccessDenied
        case saveFailed(Error)
        
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
            }
        }
    }
    
    /// Captures a screenshot from the Windows PC
    /// - Parameter baseURL: The base URL of the screenshot server (e.g., "http://192.168.1.100:5000")
    /// - Returns: The captured screenshot as UIImage
    func captureScreenshot(baseURL: String) async throws -> UIImage {
        guard let url = URL(string: "\(baseURL)/api/screenshot") else {
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
    
    /// Saves an image to the Photos library
    /// - Parameter image: The image to save
    func saveToPhotos(_ image: UIImage) async throws {
        let status = await PHPhotoLibrary.requestAuthorization(for: .addOnly)
        
        guard status == .authorized || status == .limited else {
            throw ScreenshotError.photoLibraryAccessDenied
        }
        
        try await withCheckedThrowingContinuation { (continuation: CheckedContinuation<Void, Error>) in
            PHPhotoLibrary.shared().performChanges {
                let request = PHAssetCreationRequest.forAsset()
                if let imageData = image.pngData() {
                    request.addResource(with: .photo, data: imageData, options: nil)
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
