using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Screenshotter.Windows.Services;

/// <summary>
/// Hosts a Minimal API server for screenshot capture functionality.
/// </summary>
public static class ApiServerService
{
    private const int Port = 5000;

    /// <summary>
    /// Starts the API server asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the server.</param>
    public static async Task StartAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            Args = [$"--urls=http://0.0.0.0:{Port}"]
        });

        // Add services
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonContext.Default);
        });

        var app = builder.Build();

        // Configure endpoints
        ConfigureEndpoints(app);

        // Run the server
        await app.RunAsync(cancellationToken);
    }

    private static void ConfigureEndpoints(WebApplication app)
    {
        // GET /api/info - Returns server status and IP information
        app.MapGet("/api/info", () =>
        {
            var ip = NetworkService.GetLocalIPAddress() ?? "unknown";
            return Results.Ok(new ApiInfoResponse(
                Status: "running",
                Ip: ip,
                Port: Port,
                Timestamp: DateTime.UtcNow
            ));
        })
        .WithName("GetInfo")
        .WithOpenApi();

        // GET /api/monitors - Returns list of allowed monitors
        app.MapGet("/api/monitors", () =>
        {
            try
            {
                var monitors = MonitorSettingsService.GetAllowedMonitors();
                var allowAll = MonitorSettingsService.AllowAll;
                return Results.Ok(new MonitorsResponse(monitors, allowAll));
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to enumerate monitors: {ex.Message}");
            }
        })
        .WithName("GetMonitors")
        .WithOpenApi();

        // POST /api/screenshot - Captures and returns screenshot
        // Optional query param: ?monitor={id} where id is monitor index, or "all" for all screens
        app.MapPost("/api/screenshot", (HttpRequest request) =>
        {
            try
            {
                var monitorId = request.Query["monitor"].FirstOrDefault();
                
                // Check if "all" is requested but not allowed
                if (monitorId?.Equals("all", StringComparison.OrdinalIgnoreCase) == true && 
                    !MonitorSettingsService.AllowAll)
                {
                    return Results.Problem("Capture all monitors is not allowed");
                }

                // Check if specific monitor is allowed
                if (!string.IsNullOrEmpty(monitorId) && 
                    !monitorId.Equals("all", StringComparison.OrdinalIgnoreCase) &&
                    !MonitorSettingsService.IsMonitorAllowed(monitorId))
                {
                    return Results.Problem($"Monitor {monitorId} is not allowed");
                }

                var screenshotBytes = ScreenshotService.CaptureMonitor(monitorId);

                if (screenshotBytes == null || screenshotBytes.Length == 0)
                {
                    return Results.Problem("Failed to capture screenshot");
                }

                return Results.File(screenshotBytes, "image/png", "screenshot.png");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Screenshot capture failed: {ex.Message}");
            }
        })
        .WithName("CaptureScreenshot")
        .WithOpenApi();

        // GET /api/health - Simple health check endpoint
        app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }))
            .WithName("HealthCheck");
    }
}

/// <summary>
/// Response model for the /api/info endpoint.
/// </summary>
public record ApiInfoResponse(
    string Status,
    string Ip,
    int Port,
    DateTime Timestamp
);

/// <summary>
/// Response model for the /api/monitors endpoint.
/// </summary>
public record MonitorsResponse(
    List<MonitorInfo> Monitors,
    bool AllowCaptureAll
);

/// <summary>
/// JSON serialization context for API models (AOT compatible).
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(ApiInfoResponse))]
[System.Text.Json.Serialization.JsonSerializable(typeof(MonitorsResponse))]
[System.Text.Json.Serialization.JsonSerializable(typeof(List<MonitorInfo>))]
[System.Text.Json.Serialization.JsonSerializable(typeof(MonitorInfo))]
[System.Text.Json.Serialization.JsonSerializable(typeof(object))]
public partial class ApiJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}
