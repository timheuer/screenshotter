namespace Screenshotter.Windows.Services;

/// <summary>
/// Manages which monitors are allowed to be captured by the iOS app.
/// </summary>
public static class MonitorSettingsService
{
    private static readonly HashSet<string> _allowedMonitorIds = [];
    private static bool _allowAll = true; // Default to allowing all monitors

    /// <summary>
    /// Gets or sets whether all monitors can be captured (including "capture all" option).
    /// </summary>
    public static bool AllowAll
    {
        get => _allowAll;
        set => _allowAll = value;
    }

    /// <summary>
    /// Gets the set of allowed monitor IDs.
    /// </summary>
    public static IReadOnlySet<string> AllowedMonitorIds => _allowedMonitorIds;

    /// <summary>
    /// Sets whether a specific monitor is allowed.
    /// </summary>
    public static void SetMonitorAllowed(string monitorId, bool allowed)
    {
        if (allowed)
        {
            _allowedMonitorIds.Add(monitorId);
        }
        else
        {
            _allowedMonitorIds.Remove(monitorId);
        }
    }

    /// <summary>
    /// Checks if a specific monitor is allowed.
    /// </summary>
    public static bool IsMonitorAllowed(string monitorId)
    {
        // If AllowAll is true, all monitors are allowed
        if (_allowAll)
        {
            return true;
        }

        return _allowedMonitorIds.Contains(monitorId);
    }

    /// <summary>
    /// Initializes with all monitors allowed by default.
    /// </summary>
    public static void InitializeWithMonitors(IEnumerable<MonitorInfo> monitors)
    {
        _allowedMonitorIds.Clear();
        foreach (var monitor in monitors)
        {
            _allowedMonitorIds.Add(monitor.Id);
        }
    }

    /// <summary>
    /// Gets the filtered list of monitors that are allowed to be captured.
    /// </summary>
    public static List<MonitorInfo> GetAllowedMonitors()
    {
        var allMonitors = ScreenshotService.GetMonitors();
        
        if (_allowAll)
        {
            return allMonitors;
        }

        return allMonitors.Where(m => _allowedMonitorIds.Contains(m.Id)).ToList();
    }
}
