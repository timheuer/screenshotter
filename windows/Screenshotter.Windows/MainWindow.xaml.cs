using System.Collections.ObjectModel;
using System.ComponentModel;
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
/// View model for monitor selection in the UI.
/// </summary>
public class MonitorViewModel : INotifyPropertyChanged
{
    private bool _isAllowed = true;

    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    
    public bool IsAllowed
    {
        get => _isAllowed;
        set
        {
            if (_isAllowed != value)
            {
                _isAllowed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAllowed)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Main window displaying QR code and server status.
/// </summary>
public sealed partial class MainWindow : Window
{
    private const string AppName = "Screenshotter";
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const int ApiPort = 5000;
    private const int WindowWidth = 380;
    private const int WindowHeight = 820;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern int GetDpiForWindow(IntPtr hwnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private IntPtr _hwnd;
    private AppWindow? _appWindow;
    private double _currentDpiScale = 1.0;
    private readonly ObservableCollection<MonitorViewModel> _monitorViewModels = [];

    public MainWindow()
    {
        InitializeComponent();

        // Configure window size and appearance
        ConfigureWindow();

        // Load initial state
        LoadStartupSetting();

        // Initialize monitor list
        RefreshMonitorList();

        // Refresh IP and QR code
        RefreshNetworkInfo();
    }

    private void ConfigureWindow()
    {
        // Get the AppWindow for this window
        _hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        // Set the app icon
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "TrayIcon.png");
        if (File.Exists(iconPath))
        {
            _appWindow.SetIcon(iconPath);
        }

        // Get current DPI and resize with proper scaling
        UpdateDpiScale();
        ResizeWindowForDpi();

        // Subscribe to window changes to detect DPI changes when moving between monitors
        _appWindow.Changed += AppWindow_Changed;

        // Disable resizing by using OverlappedPresenter
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
        }

        // Set title
        _appWindow.Title = "Screenshotter";
    }

    private void UpdateDpiScale()
    {
        var dpi = GetDpiForWindow(_hwnd);
        _currentDpiScale = dpi > 0 ? dpi / 96.0 : 1.0; // 96 DPI is baseline, fallback to 1.0 if invalid
    }

    private void ResizeWindowForDpi()
    {
        if (_appWindow == null) return;

        // Scale the window dimensions based on current DPI
        var scaledWidth = (int)(WindowWidth * _currentDpiScale);
        var scaledHeight = (int)(WindowHeight * _currentDpiScale);
        _appWindow.Resize(new SizeInt32(scaledWidth, scaledHeight));
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        // Check if the window moved (could be to a different monitor with different DPI)
        if (args.DidPositionChange)
        {
            var previousDpiScale = _currentDpiScale;
            UpdateDpiScale();

            // Only resize if DPI actually changed (moved to different DPI monitor)
            if (Math.Abs(previousDpiScale - _currentDpiScale) > 0.01)
            {
                ResizeWindowForDpi();
            }
        }
    }

    /// <summary>
    /// Positions the window above the system tray (near the cursor position).
    /// </summary>
    public void PositionNearTray()
    {
        if (_appWindow == null) return;

        var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
        if (displayArea == null) return;

        // Get cursor position (where user clicked the tray icon)
        GetCursorPos(out var cursorPos);

        // Update DPI for the target display and resize if needed
        var previousDpiScale = _currentDpiScale;
        UpdateDpiScale();
        if (Math.Abs(previousDpiScale - _currentDpiScale) > 0.01)
        {
            ResizeWindowForDpi();
        }

        // Calculate scaled dimensions for positioning
        var scaledWidth = (int)(WindowWidth * _currentDpiScale);
        var scaledHeight = (int)(WindowHeight * _currentDpiScale);

        // Calculate position: above the cursor, aligned to right edge of work area
        var workArea = displayArea.WorkArea;
        
        // Position window so it appears above the tray area
        // Typically tray is at bottom-right, so position window above and to the left of cursor
        var x = Math.Min(cursorPos.X - (scaledWidth / 2), workArea.Width - scaledWidth + workArea.X);
        x = Math.Max(x, workArea.X); // Don't go off left edge
        
        var y = cursorPos.Y - scaledHeight - 10; // 10px padding above cursor
        if (y < workArea.Y)
        {
            // If would go above screen, position below cursor instead
            y = cursorPos.Y + 10;
        }

        _appWindow.Move(new PointInt32(x, y));
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
        RefreshMonitorList();
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        AppWindow.Hide();
    }

    private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SetStartupSetting(StartupToggle.IsOn);
    }

    private void RefreshMonitorList()
    {
        var monitors = ScreenshotService.GetMonitors();
        
        // Initialize settings service with all monitors
        MonitorSettingsService.InitializeWithMonitors(monitors);

        // Update view models
        _monitorViewModels.Clear();
        foreach (var monitor in monitors)
        {
            _monitorViewModels.Add(new MonitorViewModel
            {
                Id = monitor.Id,
                DisplayName = $"{monitor.Name} ({monitor.Width}x{monitor.Height})",
                IsAllowed = MonitorSettingsService.IsMonitorAllowed(monitor.Id)
            });
        }

        // Guard against null during initialization
        if (MonitorList != null)
        {
            MonitorList.ItemsSource = _monitorViewModels;
        }
        
        UpdateMonitorListVisibility();
    }

    private void UpdateMonitorListVisibility()
    {
        // Guard against null during initialization
        if (MonitorList == null || AllowAllToggle == null) return;
        
        // Hide individual monitor list when "Allow All" is enabled
        MonitorList.Visibility = AllowAllToggle.IsOn ? Visibility.Collapsed : Visibility.Visible;
    }

    private void AllowAllToggle_Toggled(object sender, RoutedEventArgs e)
    {
        MonitorSettingsService.AllowAll = AllowAllToggle.IsOn;
        UpdateMonitorListVisibility();
    }

    private void MonitorCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // Sync the UI state to the settings service
        foreach (var vm in _monitorViewModels)
        {
            MonitorSettingsService.SetMonitorAllowed(vm.Id, vm.IsAllowed);
        }
    }
}
