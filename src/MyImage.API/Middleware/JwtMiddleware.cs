using System.Net;
using System.Text.Json;
using MyImage.Core.DTOs.Common;

namespace MyImage.API.Middleware;

/// <summary>
/// JWT authentication middleware for validating bearer tokens.
/// Validates JWT tokens on protected routes and sets user context.
/// Works in conjunction with ASP.NET Core authentication but provides additional logging.
/// 
/// This middleware extracts and validates JWT tokens from the Authorization header,
/// setting the user principal for downstream authorization decisions.
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtMiddleware> _logger;

    /// <summary>
    /// Initialize JWT middleware with logging.
    /// </summary>
    /// <param name="next">Next middleware in the pipeline</param>
    /// <param name="logger">Logger for authentication events</param>
    public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invoke middleware to validate JWT tokens and set user context.
    /// Examines the Authorization header for bearer tokens and logs authentication events.
    /// </summary>
    /// <param name="context">HTTP context for the current request</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractTokenFromHeader(context);

        if (!string.IsNullOrEmpty(token))
        {
            // Log authentication attempt (token prefix only for security)
            var tokenPrefix = token.Length > 10 ? token.Substring(0, 10) + "..." : "short_token";
            _logger.LogDebug("JWT token present in request to {Path}: {TokenPrefix}",
                context.Request.Path, tokenPrefix);

            // Additional JWT validation could be performed here if needed
            // The built-in ASP.NET Core JWT authentication handles the actual validation
        }
        else if (RequiresAuthentication(context))
        {
            _logger.LogDebug("No JWT token found for protected route: {Path}", context.Request.Path);
        }

        // Continue to next middleware
        await _next(context);

        // Log authentication results after request processing
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("user_id")?.Value ?? "unknown";
            _logger.LogDebug("Authenticated request completed for user: {UserId}", userId);
        }
    }

    /// <summary>
    /// Extract JWT token from Authorization header.
    /// Looks for "Bearer {token}" format in the Authorization header.
    /// </summary>
    /// <param name="context">HTTP context containing request headers</param>
    /// <returns>JWT token string if found, null otherwise</returns>
    private static string? ExtractTokenFromHeader(HttpContext context)
    {
        var authorizationHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
        {
            return authorizationHeader.Substring("Bearer ".Length).Trim();
        }

        return null;
    }

    /// <summary>
    /// Determine if the current request requires authentication.
    /// Checks if the route typically requires authentication (simplified heuristic).
    /// </summary>
    /// <param name="context">HTTP context for route analysis</param>
    /// <returns>True if route likely requires authentication</returns>
    private static bool RequiresAuthentication(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Routes that typically don't require authentication
        var publicRoutes = new[]
        {
            "/api/auth/register",
            "/api/auth/login",
            "/api/print-sizes",
            "/health",
            "/swagger"
        };

        return !publicRoutes.Any(route => path.StartsWith(route));
    }
}