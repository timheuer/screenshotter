using System.Drawing;
using H.NotifyIcon;
using H.NotifyIcon.Core;

namespace Screenshotter.Windows.Services;

/// <summary>
/// Manages the system tray icon using H.NotifyIcon.WinUI.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly TaskbarIcon _trayIcon;
    private readonly Action _onShowWindow;
    private readonly Action _onExit;
    private readonly Icon? _icon;
    private bool _disposed;

    public TaskbarIcon TrayIcon => _trayIcon;

    public TrayIconService(Action onShowWindow, Action onExit)
    {
        _onShowWindow = onShowWindow;
        _onExit = onExit;

        // Load the app icon from Assets
        _icon = LoadAppIcon();

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Screenshotter - Click to show QR code",
            NoLeftClickDelay = true,
            Icon = _icon
        };

        // Handle left click to show window
        _trayIcon.LeftClickCommand = new RelayCommand(() => onShowWindow());

        // Create context menu using WinUI MenuFlyout
        var contextMenu = new Microsoft.UI.Xaml.Controls.MenuFlyout();

        var showItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Show QR Code" };
        showItem.Command = new RelayCommand(() => onShowWindow());
        contextMenu.Items.Add(showItem);

        contextMenu.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutSeparator());

        var quitItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Quit" };
        quitItem.Command = new RelayCommand(() =>
        {
            // Terminate immediately - use Environment.FailFast for guaranteed termination
            //App.Current.Exit();
            Environment.FailFast("User requested application exit");
        });
        contextMenu.Items.Add(quitItem);

        _trayIcon.ContextFlyout = contextMenu;
    }

    private static Icon? LoadAppIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "TrayIcon.png");
            if (File.Exists(iconPath))
            {
                using var bitmap = new Bitmap(iconPath);
                // Resize to appropriate tray icon size (32x32 or 16x16)
                using var resized = new Bitmap(bitmap, new Size(32, 32));
                return Icon.FromHandle(resized.GetHicon());
            }
        }
        catch
        {
            // Fall back to default icon
        }
        return SystemIcons.Application;
    }

    public void ForceCreate()
    {
        _trayIcon.ForceCreate();
    }

    public void ShowNotification(string title, string text)
    {
        _trayIcon.ShowNotification(title, text, NotificationIcon.Info);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _trayIcon.Dispose();
    }
}

/// <summary>
/// Simple relay command implementation for tray icon commands.
/// </summary>
internal sealed class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}
