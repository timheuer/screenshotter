using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using Screenshotter.Windows.Services;
using Windows.Storage.Streams;

namespace Screenshotter.Windows;

/// <summary>
/// Main window displaying QR code and server status.
/// </summary>
public sealed partial class MainWindow : Window
{
    private const string AppName = "Screenshotter";
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const int ApiPort = 5000;

    public MainWindow()
    {
        InitializeComponent();

        // Load initial state
        LoadStartupSetting();

        // Refresh IP and QR code
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
}
