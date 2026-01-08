using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using Screenshotter.Windows.Services;
using Windows.Storage.Streams;

namespace Screenshotter.Windows;

/// <summary>
/// Main window that hosts the system tray icon and popup UI.
/// The window itself stays hidden - only the tray icon is visible.
/// </summary>
public sealed partial class MainWindow : Window
{
    private const string AppName = "Screenshotter";
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const int ApiPort = 5000;

    public ICommand ShowPopupCommand { get; }

    public MainWindow()
    {
        InitializeComponent();

        ShowPopupCommand = new RelayCommand(OnShowPopup);

        // Generate tray icon
        GenerateTrayIcon();

        // Load initial state
        LoadStartupSetting();

        // Refresh IP and QR code
        RefreshNetworkInfo();
    }

    private void GenerateTrayIcon()
    {
        // Create a simple icon using the camera emoji rendered to bitmap
        try
        {
            var iconBytes = IconGenerator.CreateCameraIcon();
            using var stream = new MemoryStream(iconBytes);
            var icon = new System.Drawing.Icon(stream);
            TrayIcon.Icon = icon;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to generate icon: {ex.Message}");
        }
    }

    private void OnShowPopup()
    {
        // Refresh network info when popup is shown
        RefreshNetworkInfo();
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

    private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SetStartupSetting(StartupToggle.IsOn);
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        TrayIcon.Dispose();

        if (Application.Current is App app)
        {
            app.Shutdown();
        }
    }
}

/// <summary>
/// Simple relay command implementation for MVVM binding.
/// </summary>
public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// Helper class to generate a simple camera icon for the system tray.
/// </summary>
public static class IconGenerator
{
    public static byte[] CreateCameraIcon()
    {
        const int size = 32;

        using var bitmap = new System.Drawing.Bitmap(size, size);
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);

        // Set high quality rendering
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        // Fill background with transparent
        graphics.Clear(System.Drawing.Color.Transparent);

        // Draw camera body (rounded rectangle)
        using var bodyBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(100, 149, 237)); // Cornflower blue
        using var bodyPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(65, 105, 225), 1); // Royal blue border

        var bodyRect = new System.Drawing.Rectangle(2, 8, 28, 20);
        graphics.FillRectangle(bodyBrush, bodyRect);
        graphics.DrawRectangle(bodyPen, bodyRect);

        // Draw camera lens (circle)
        using var lensBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(30, 30, 30));
        using var lensHighlight = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(80, 80, 80));

        var lensRect = new System.Drawing.Rectangle(9, 11, 14, 14);
        graphics.FillEllipse(lensBrush, lensRect);

        // Lens highlight
        var highlightRect = new System.Drawing.Rectangle(11, 13, 6, 6);
        graphics.FillEllipse(lensHighlight, highlightRect);

        // Draw flash (small rectangle on top)
        using var flashBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 200, 0));
        var flashRect = new System.Drawing.Rectangle(20, 4, 8, 4);
        graphics.FillRectangle(flashBrush, flashRect);

        // Convert to icon
        using var stream = new MemoryStream();

        // Create icon from bitmap
        var iconHandle = bitmap.GetHicon();
        using var icon = System.Drawing.Icon.FromHandle(iconHandle);
        icon.Save(stream);

        return stream.ToArray();
    }
}
