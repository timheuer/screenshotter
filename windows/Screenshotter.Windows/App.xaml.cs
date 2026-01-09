using Microsoft.UI.Xaml;
using Screenshotter.Windows.Services;

namespace Screenshotter.Windows;

/// <summary>
/// Main application class that initializes the system tray application
/// and starts the background API server.
/// </summary>
public partial class App : Application
{
    private MainWindow? _window;
    private CancellationTokenSource? _apiServerCts;
    private TrayIconService? _trayIcon;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Start the API server as a background task
        StartApiServer();

        // Create the main window - must activate briefly to initialize WinUI properly
        _window = new MainWindow();
        _window.Closed += Window_Closed;
        
        // Activate window briefly to ensure message loop is running,
        // then hide it. This prevents intermittent startup failures.
        _window.Activate();

        // Initialize system tray icon after window is activated
        _trayIcon = new TrayIconService(
            onShowWindow: ShowWindow,
            onExit: Shutdown
        );

        // Force create the tray icon
        _trayIcon.ForceCreate();

        // Now hide the window to tray
        _window.AppWindow.Hide();

        // Show notification on startup
        _trayIcon.ShowNotification("Screenshotter", "Running in system tray. Click to show QR code.");
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        // Prevent window from actually closing - just hide it
        args.Handled = true;
        _window?.AppWindow.Hide();
    }

    public void ShowWindow()
    {
        if (_window == null) return;

        // Position window near the tray icon before showing
        _window.PositionNearTray();
        _window.AppWindow.Show();
        _window.Activate();
    }

    private void StartApiServer()
    {
        _apiServerCts = new CancellationTokenSource();
        var token = _apiServerCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await ApiServerService.StartAsync(token);
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Server error: {ex.Message}");
            }
        }, token);
    }

    public void Shutdown()
    {
        // Unhook the closed handler so window can actually close
        if (_window != null)
        {
            _window.Closed -= Window_Closed;
        }

        _apiServerCts?.Cancel();
        _apiServerCts?.Dispose();
        _trayIcon?.Dispose();

        try
        {
            _window?.Close();
        }
        catch
        {
            // Ignore any errors during window close
        }

        // Force terminate the process
        System.Diagnostics.Process.GetCurrentProcess().Kill();
    }
}
