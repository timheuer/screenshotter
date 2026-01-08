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

        // POST /api/screenshot - Captures and returns screenshot
        app.MapPost("/api/screenshot", () =>
        {
            try
            {
                var screenshotBytes = ScreenshotService.CaptureScreen();

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
/// JSON serialization context for API models (AOT compatible).
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(ApiInfoResponse))]
[System.Text.Json.Serialization.JsonSerializable(typeof(object))]
public partial class ApiJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}
