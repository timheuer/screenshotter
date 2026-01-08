using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using Screenshotter.Windows.Services;
using Windows.Graphics;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace Screenshotter.Windows;

/// <summary>
/// Main window displaying QR code and server status.
/// </summary>
public sealed partial class MainWindow : Window
{
    private const string AppName = "Screenshotter";
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const int ApiPort = 5000;
    private const int WindowWidth = 380;
    private const int WindowHeight = 700;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    public MainWindow()
    {
        InitializeComponent();

        // Configure window size and appearance
        ConfigureWindow();

        // Load initial state
        LoadStartupSetting();

        // Refresh IP and QR code
        RefreshNetworkInfo();
    }

    private void ConfigureWindow()
    {
        // Get the AppWindow for this window
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // Set the app icon
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "TrayIcon.png");
        if (File.Exists(iconPath))
        {
            appWindow.SetIcon(iconPath);
        }

        // Set fixed size
        appWindow.Resize(new SizeInt32(WindowWidth, WindowHeight));

        // Disable resizing by using OverlappedPresenter
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
        }

        // Set title
        appWindow.Title = "Screenshotter";
    }

    /// <summary>
    /// Positions the window above the system tray (near the cursor position).
    /// </summary>
    public void PositionNearTray()
    {
        var appWindow = AppWindow;
        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        if (displayArea == null) return;

        // Get cursor position (where user clicked the tray icon)
        GetCursorPos(out var cursorPos);

        // Calculate position: above the cursor, aligned to right edge of work area
        var workArea = displayArea.WorkArea;
        
        // Position window so it appears above the tray area
        // Typically tray is at bottom-right, so position window above and to the left of cursor
        var x = Math.Min(cursorPos.X - (WindowWidth / 2), workArea.Width - WindowWidth + workArea.X);
        x = Math.Max(x, workArea.X); // Don't go off left edge
        
        var y = cursorPos.Y - WindowHeight - 10; // 10px padding above cursor
        if (y < workArea.Y)
        {
            // If would go above screen, position below cursor instead
            y = cursorPos.Y + 10;
        }

        appWindow.Move(new PointInt32(x, y));
    }

    private void RefreshNetworkInfo()
    {
        try
        {
            var ip = NetworkService.GetLocalIPAddress();
            IpAddressText.Text = ip ?? "Not available";

            if (!string.IsNullOrEmpty(ip))
            {
                var qrBytes = QrCodeService.GenerateQrCode(ip, ApiPort);
                SetQrCodeImage(qrBytes);
                StatusText.Text = "Running";
                StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.Colors.Green);
            }
            else
            {
                StatusText.Text = "No network";
                StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.Colors.Red);
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error";
            StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Red);
            System.Diagnostics.Debug.WriteLine($"Network refresh error: {ex.Message}");
        }
    }

    private async void SetQrCodeImage(byte[] pngBytes)
    {
        try
        {
            var bitmapImage = new BitmapImage();
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(pngBytes.AsBuffer());
            stream.Seek(0);
            await bitmapImage.SetSourceAsync(stream);
            QrCodeImage.Source = bitmapImage;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set QR code image: {ex.Message}");
        }
    }

    private void LoadStartupSetting()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
            var value = key?.GetValue(AppName);
            StartupToggle.IsOn = value != null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load startup setting: {ex.Message}");
            StartupToggle.IsOn = false;
        }
    }

    private void SetStartupSetting(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (key == null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set startup setting: {ex.Message}");
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshNetworkInfo();
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        AppWindow.Hide();
    }

    private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SetStartupSetting(StartupToggle.IsOn);
    }
}
