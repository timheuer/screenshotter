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

                // Check if "all" is requested - redirect to separate capture endpoint
                if (monitorId?.Equals("all", StringComparison.OrdinalIgnoreCase) == true)
                {
                    if (!MonitorSettingsService.AllowAll)
                    {
                        return Results.Problem("Capture all monitors is not allowed");
                    }
                    // For backwards compatibility, return stitched image
                    var allScreensBytes = ScreenshotService.CaptureAllScreens();
                    if (allScreensBytes == null || allScreensBytes.Length == 0)
                    {
                        return Results.Problem("Failed to capture all screens");
                    }
                    return Results.File(allScreensBytes, "image/png", "screenshot_all.png");
                }

                // Check if specific monitor is allowed
                if (!string.IsNullOrEmpty(monitorId) &&
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

        // POST /api/screenshot/all-separate - Captures all monitors as separate images
        // Returns JSON array with base64-encoded images
        app.MapPost("/api/screenshot/all-separate", () =>
        {
            try
            {
                if (!MonitorSettingsService.AllowAll)
                {
                    return Results.Problem("Capture all monitors is not allowed");
                }

                var screenshots = ScreenshotService.CaptureAllMonitorsSeparately();

                if (screenshots.Count == 0)
                {
                    return Results.Problem("Failed to capture any monitors");
                }

                var response = screenshots.Select(s => new SeparateScreenshotResponse(
                    MonitorId: s.MonitorId,
                    MonitorName: s.MonitorName,
                    ImageBase64: Convert.ToBase64String(s.ImageData)
                )).ToList();

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Screenshot capture failed: {ex.Message}");
            }
        })
        .WithName("CaptureAllSeparate")
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
/// Response model for individual screenshot in /api/screenshot/all-separate endpoint.
/// </summary>
public record SeparateScreenshotResponse(
    string MonitorId,
    string MonitorName,
    string ImageBase64
);

/// <summary>
/// JSON serialization context for API models (AOT compatible).
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(ApiInfoResponse))]
[System.Text.Json.Serialization.JsonSerializable(typeof(MonitorsResponse))]
[System.Text.Json.Serialization.JsonSerializable(typeof(List<MonitorInfo>))]
[System.Text.Json.Serialization.JsonSerializable(typeof(MonitorInfo))]
[System.Text.Json.Serialization.JsonSerializable(typeof(List<SeparateScreenshotResponse>))]
[System.Text.Json.Serialization.JsonSerializable(typeof(SeparateScreenshotResponse))]
[System.Text.Json.Serialization.JsonSerializable(typeof(object))]
public partial class ApiJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}
