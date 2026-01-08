using Microsoft.UI.Xaml;
using Screenshotter.Windows.Services;

namespace Screenshotter.Windows;

/// <summary>
/// Main application class that initializes the system tray application
/// and starts the background API server.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    private CancellationTokenSource? _apiServerCts;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Start the API server as a background task
        StartApiServer();

        // Create and show the main window
        _window = new MainWindow();
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
        _apiServerCts?.Cancel();
        _apiServerCts?.Dispose();
        _window?.Close();
        Exit();
    }
}
