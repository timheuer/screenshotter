using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Screenshotter.Windows.Services;

/// <summary>
/// Service for network-related operations.
/// </summary>
public static class NetworkService
{
    /// <summary>
    /// Gets the local IPv4 address of the first active network interface.
    /// Skips loopback and virtual adapters.
    /// </summary>
    /// <returns>The local IP address as a string, or null if not found.</returns>
    public static string? GetLocalIPAddress()
    {
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Where(ni => !ni.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase))
                .Where(ni => !ni.Description.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                .ThenByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);

            foreach (var networkInterface in networkInterfaces)
            {
                var ipProperties = networkInterface.GetIPProperties();

                // Skip interfaces without gateway (usually not connected to network)
                if (ipProperties.GatewayAddresses.Count == 0)
                    continue;

                var ipv4Address = ipProperties.UnicastAddresses
                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Where(ua => !IPAddress.IsLoopback(ua.Address))
                    .Select(ua => ua.Address.ToString())
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(ipv4Address))
                {
                    return ipv4Address;
                }
            }

            // Fallback: try to get IP by connecting to external address
            return GetIPAddressBySocket();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get local IP: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Fallback method to get local IP by creating a socket connection.
    /// </summary>
    private static string? GetIPAddressBySocket()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 53);

            if (socket.LocalEndPoint is IPEndPoint endPoint)
            {
                return endPoint.Address.ToString();
            }
        }
        catch
        {
            // Ignore errors in fallback
        }

        return null;
    }
}
