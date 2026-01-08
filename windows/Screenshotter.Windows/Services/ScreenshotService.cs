using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Screenshotter.Windows.Services;

/// <summary>
/// Service for capturing screenshots using System.Drawing.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ScreenshotService
{
    static ScreenshotService()
    {
        // Enable per-monitor DPI awareness for accurate screen capture
        SetProcessDPIAware();
    }

    /// <summary>
    /// Captures the primary screen and returns it as a PNG byte array.
    /// </summary>
    /// <returns>PNG image as byte array, or null if capture fails.</returns>
    public static byte[]? CaptureScreen()
    {
        try
        {
            // Get primary screen bounds in physical pixels
            var screenBounds = GetPrimaryScreenBounds();

            if (screenBounds.Width <= 0 || screenBounds.Height <= 0)
            {
                System.Diagnostics.Debug.WriteLine("Invalid screen bounds");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"Capturing screen: {screenBounds.Width}x{screenBounds.Height}");

            // Create bitmap to hold the screenshot
            using var bitmap = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);

            // Capture the screen
            graphics.CopyFromScreen(
                screenBounds.X,
                screenBounds.Y,
                0,
                0,
                screenBounds.Size,
                CopyPixelOperation.SourceCopy
            );

            // Convert to PNG byte array
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

        var monitorInfo = new MONITORINFO();
        monitorInfo.cbSize = Marshal.SizeOf<MONITORINFO>();

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

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, int dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

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
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    #endregion
}
