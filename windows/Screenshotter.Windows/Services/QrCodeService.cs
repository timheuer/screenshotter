using System.Text.Json;
using QRCoder;

namespace Screenshotter.Windows.Services;

/// <summary>
/// Service for generating QR codes.
/// </summary>
public static class QrCodeService
{
    /// <summary>
    /// Generates a QR code PNG containing connection information.
    /// </summary>
    /// <param name="ip">The IP address to encode.</param>
    /// <param name="port">The port number to encode.</param>
    /// <returns>PNG image as byte array.</returns>
    public static byte[] GenerateQrCode(string ip, int port)
    {
        // Create JSON payload with connection info
        var payload = new ConnectionInfo(ip, port);
        var jsonPayload = JsonSerializer.Serialize(payload, JsonContext.Default.ConnectionInfo);

        // Generate QR code
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(jsonPayload, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);

        // Generate PNG with specified pixels per module (size)
        return qrCode.GetGraphic(5, darkColorRgba: [0, 0, 0, 255], lightColorRgba: [255, 255, 255, 255]);
    }
}

/// <summary>
/// Connection information to encode in QR code.
/// </summary>
public record ConnectionInfo(
    [property: System.Text.Json.Serialization.JsonPropertyName("ip")] string Ip,
    [property: System.Text.Json.Serialization.JsonPropertyName("port")] int Port);

/// <summary>
/// JSON serialization context for AOT compatibility.
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(ConnectionInfo))]
public partial class JsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}
