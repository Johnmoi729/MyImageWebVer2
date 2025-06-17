using System.Net;
using System.Text.Json;
using MyImage.Core.DTOs.Common;

namespace MyImage.API.Middleware;

/// <summary>
/// Request logging middleware for API monitoring and debugging.
/// Logs all incoming requests with timing information for performance monitoring.
/// Useful for debugging and understanding API usage patterns.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>
    /// Initialize request logging middleware.
    /// </summary>
    /// <param name="next">Next middleware in the pipeline</param>
    /// <param name="logger">Logger for request information</param>
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invoke middleware to log request details and timing.
    /// Records request method, path, user agent, and response time.
    /// </summary>
    /// <param name="context">HTTP context for the current request</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Extract useful request information
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
        var clientIp = GetClientIpAddress(context);

        // Log incoming request
        _logger.LogInformation("Incoming {Method} request to {Path}{QueryString} from {ClientIp}",
            method, path, queryString, clientIp);

        try
        {
            // Continue to next middleware
            await _next(context);
        }
        finally
        {
            // Log request completion with timing
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var duration = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Completed {Method} {Path} with status {StatusCode} in {Duration}ms",
                method, path, statusCode, duration);

            // Log slow requests for performance monitoring
            if (duration > 5000) // 5 seconds threshold
            {
                _logger.LogWarning("Slow request detected: {Method} {Path} took {Duration}ms",
                    method, path, duration);
            }
        }
    }

    /// <summary>
    /// Extract client IP address from request headers and connection info.
    /// Handles various proxy and load balancer scenarios.
    /// </summary>
    /// <param name="context">HTTP context containing connection information</param>
    /// <returns>Client IP address</returns>
    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take first IP if multiple are present
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}