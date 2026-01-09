using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Screenshotter.Windows.Services;

/// <summary>
/// Information about a display monitor.
/// </summary>
public record MonitorInfo(
    string Id,
    string Name,
    int Width,
    int Height,
    int X,
    int Y,
    bool IsPrimary
);

/// <summary>
/// Represents a captured screenshot with monitor info.
/// </summary>
public record ScreenshotResult(
    string MonitorId,
    string MonitorName,
    byte[] ImageData
);

/// <summary>
/// Service for capturing screenshots using System.Drawing.
/// Uses thread-level DPI awareness for accurate capture without affecting the WinUI window.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ScreenshotService
{
    private static readonly List<MonitorData> _monitors = [];

    /// <summary>
    /// Gets information about all connected monitors.
    /// </summary>
    public static List<MonitorInfo> GetMonitors()
    {
        // Use thread-level DPI awareness for accurate monitor enumeration
        var previousContext = SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        try
        {
            _monitors.Clear();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumCallback, IntPtr.Zero);

            var result = new List<MonitorInfo>();
            for (int i = 0; i < _monitors.Count; i++)
            {
                var monitor = _monitors[i];
                var bounds = monitor.Bounds;

                // Try to get a friendly name
                var friendlyName = GetMonitorFriendlyName(monitor.Handle) ?? $"Display {i + 1}";

                result.Add(new MonitorInfo(
                    Id: i.ToString(),
                    Name: monitor.IsPrimary ? $"{friendlyName} (Primary)" : friendlyName,
                    Width: bounds.Width,
                    Height: bounds.Height,
                    X: bounds.X,
                    Y: bounds.Y,
                    IsPrimary: monitor.IsPrimary
                ));
            }

            return result;
        }
        finally
        {
            // Restore previous DPI awareness context
            if (previousContext != IntPtr.Zero)
            {
                SetThreadDpiAwarenessContext(previousContext);
            }
        }
    }

    /// <summary>
    /// Captures a specific monitor by ID, or the primary monitor if no ID specified.
    /// </summary>
    /// <param name="monitorId">Monitor ID (index as string), or null for primary.</param>
    /// <returns>PNG image as byte array, or null if capture fails.</returns>
    public static byte[]? CaptureMonitor(string? monitorId)
    {
        // Use thread-level DPI awareness for accurate capture
        var previousContext = SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        try
        {
            // Refresh monitor list (already uses thread DPI awareness internally)
            _monitors.Clear();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumCallback, IntPtr.Zero);

            Rectangle bounds;

            if (string.IsNullOrEmpty(monitorId))
            {
                // Default to primary monitor
                var primaryIndex = _monitors.FindIndex(m => m.IsPrimary);
                if (primaryIndex < 0) primaryIndex = 0;
                if (_monitors.Count == 0) return null;
                bounds = _monitors[primaryIndex].Bounds;
            }
            else if (monitorId.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                // Capture all screens as single stitched image
                return CaptureAllScreensInternal();
            }
            else if (int.TryParse(monitorId, out int index) && index >= 0 && index < _monitors.Count)
            {
                bounds = _monitors[index].Bounds;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Invalid monitor ID: {monitorId}");
                return null;
            }

            return CaptureRegionInternal(bounds);
        }
        finally
        {
            if (previousContext != IntPtr.Zero)
            {
                SetThreadDpiAwarenessContext(previousContext);
            }
        }
    }

    /// <summary>
    /// Captures all monitors as separate images.
    /// </summary>
    /// <returns>List of screenshot results, one per monitor.</returns>
    public static List<ScreenshotResult> CaptureAllMonitorsSeparately()
    {
        // Use thread-level DPI awareness for accurate capture
        var previousContext = SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        try
        {
            var monitors = GetMonitors();
            var results = new List<ScreenshotResult>();

            for (int i = 0; i < _monitors.Count; i++)
            {
                var monitor = _monitors[i];
                var monitorInfo = monitors[i];
                var imageData = CaptureRegionInternal(monitor.Bounds);

                if (imageData != null)
                {
                    results.Add(new ScreenshotResult(
                        MonitorId: monitorInfo.Id,
                        MonitorName: monitorInfo.Name,
                        ImageData: imageData
                    ));
                }
            }

            return results;
        }
        finally
        {
            if (previousContext != IntPtr.Zero)
            {
                SetThreadDpiAwarenessContext(previousContext);
            }
        }
    }

    /// <summary>
    /// Captures the primary screen and returns it as a PNG byte array.
    /// </summary>
    /// <returns>PNG image as byte array, or null if capture fails.</returns>
    public static byte[]? CaptureScreen()
    {
        return CaptureMonitor(null);
    }

    /// <summary>
    /// Captures a specific screen region and returns it as a PNG byte array.
    /// Must be called with thread DPI awareness already set.
    /// </summary>
    private static byte[]? CaptureRegionInternal(Rectangle bounds)
    {
        try
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                System.Diagnostics.Debug.WriteLine("Invalid screen bounds");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"Capturing region: {bounds.Width}x{bounds.Height} at ({bounds.X},{bounds.Y})");

            using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(
                bounds.X,
                bounds.Y,
                0,
                0,
                bounds.Size,
                CopyPixelOperation.SourceCopy
            );

            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot capture failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the bounds of the primary screen in physical pixels (DPI-aware).
    /// </summary>
    private static Rectangle GetPrimaryScreenBounds()
    {
        // Get the primary monitor handle
        var hMonitor = MonitorFromPoint(new POINT { x = 0, y = 0 }, MONITOR_DEFAULTTOPRIMARY);

        var monitorInfo = new MONITORINFOEX();
        monitorInfo.cbSize = Marshal.SizeOf<MONITORINFOEX>();

        if (GetMonitorInfo(hMonitor, ref monitorInfo))
        {
            var rect = monitorInfo.rcMonitor;
            return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        }

        // Fallback to GetSystemMetrics (may not be DPI-aware)
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);
        return new Rectangle(0, 0, width, height);
    }

    /// <summary>
    /// Captures all screens (virtual screen) and returns as PNG byte array.
    /// </summary>
    public static byte[]? CaptureAllScreens()
    {
        // Use thread-level DPI awareness for accurate capture
        var previousContext = SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        try
        {
            return CaptureAllScreensInternal();
        }
        finally
        {
            if (previousContext != IntPtr.Zero)
            {
                SetThreadDpiAwarenessContext(previousContext);
            }
        }
    }

    /// <summary>
    /// Captures all screens (virtual screen) and returns as PNG byte array.
    /// Must be called with thread DPI awareness already set.
    /// </summary>
    private static byte[]? CaptureAllScreensInternal()
    {
        try
        {
            int x = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int y = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            if (width <= 0 || height <= 0)
            {
                return null;
            }

            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"All screens capture failed: {ex.Message}");
            return null;
        }
    }

    #region Native Methods

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;
    private const int MONITOR_DEFAULTTOPRIMARY = 1;
    private const int MONITORINFOF_PRIMARY = 1;

    // DPI awareness constants
    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(-4);

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, int dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

    private static bool MonitorEnumCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
    {
        var monitorInfo = new MONITORINFOEX();
        monitorInfo.cbSize = Marshal.SizeOf<MONITORINFOEX>();

        if (GetMonitorInfo(hMonitor, ref monitorInfo))
        {
            var rect = monitorInfo.rcMonitor;
            var bounds = new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
            var isPrimary = (monitorInfo.dwFlags & MONITORINFOF_PRIMARY) != 0;

            _monitors.Add(new MonitorData(hMonitor, bounds, isPrimary, monitorInfo.szDevice));
        }

        return true; // Continue enumeration
    }

    private static string? GetMonitorFriendlyName(IntPtr hMonitor)
    {
        try
        {
            var monitorInfo = new MONITORINFOEX();
            monitorInfo.cbSize = Marshal.SizeOf<MONITORINFOEX>();

            if (!GetMonitorInfo(hMonitor, ref monitorInfo))
                return null;

            var deviceName = monitorInfo.szDevice;

            // Try to get the friendly name from EnumDisplayDevices
            var displayDevice = new DISPLAY_DEVICE();
            displayDevice.cb = Marshal.SizeOf<DISPLAY_DEVICE>();

            uint deviceIndex = 0;
            while (EnumDisplayDevices(deviceName, deviceIndex, ref displayDevice, 0))
            {
                if (!string.IsNullOrEmpty(displayDevice.DeviceString))
                {
                    return displayDevice.DeviceString;
                }
                deviceIndex++;
            }

            // Fallback: return the device name without the \\.\\ prefix
            if (deviceName.StartsWith(@"\\.\"))
            {
                return deviceName[4..];
            }

            return deviceName;
        }
        catch
        {
            return null;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    private record MonitorData(IntPtr Handle, Rectangle Bounds, bool IsPrimary, string DeviceName);

    #endregion
}
