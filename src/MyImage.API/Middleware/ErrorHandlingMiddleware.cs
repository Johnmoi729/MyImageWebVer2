using System.Net;
using System.Text.Json;
using MyImage.Core.DTOs.Common;

namespace MyImage.API.Middleware;

/// <summary>
/// Global error handling middleware for consistent error responses.
/// Catches all unhandled exceptions and converts them to standardized API responses.
/// Prevents sensitive error information from being exposed to clients.
/// 
/// This middleware ensures that all API endpoints return consistent error formats
/// and logs detailed error information for debugging while protecting user privacy.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initialize error handling middleware with logging and environment information.
    /// </summary>
    /// <param name="next">Next middleware in the pipeline</param>
    /// <param name="logger">Logger for error reporting</param>
    /// <param name="environment">Environment information for error detail control</param>
    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Invoke middleware to handle requests and catch exceptions.
    /// Wraps the entire request pipeline to catch any unhandled exceptions.
    /// </summary>
    /// <param name="context">HTTP context for the current request</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Continue to next middleware in pipeline
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the full exception details for debugging
            _logger.LogError(ex, "Unhandled exception occurred during request to {Path} {Method}",
                context.Request.Path, context.Request.Method);

            // Handle the exception and return appropriate response
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handle exception and generate appropriate API response.
    /// Converts exceptions to standardized error responses with appropriate HTTP status codes.
    /// </summary>
    /// <param name="context">HTTP context for response</param>
    /// <param name="exception">Exception to handle</param>
    /// <returns>Task representing the asynchronous response</returns>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Set response content type to JSON
        context.Response.ContentType = "application/json";

        // Determine appropriate status code and error message based on exception type
        var (statusCode, message, errors) = GetErrorDetails(exception);
        context.Response.StatusCode = (int)statusCode;

        // Create standardized error response
        var response = ApiResponse<object>.ErrorResponse(message, errors);

        // Include stack trace in development environment only
        if (_environment.IsDevelopment() && exception.StackTrace != null)
        {
            response.Errors.Add($"Stack Trace: {exception.StackTrace}");
        }

        // Serialize response to JSON with consistent formatting
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment() // Pretty print in development
        };

        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// Determine appropriate HTTP status code and error message for exception type.
    /// Maps different exception types to appropriate HTTP responses.
    /// </summary>
    /// <param name="exception">Exception to analyze</param>
    /// <returns>Status code, message, and error details</returns>
    private static (HttpStatusCode statusCode, string message, List<string> errors) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            // Business logic violations
            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                "Invalid operation",
                new List<string> { exception.Message }
            ),

            // Input validation errors
            ArgumentException => (
                HttpStatusCode.BadRequest,
                "Invalid input",
                new List<string> { exception.Message }
            ),

            // Authorization failures
            UnauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                "Access denied",
                new List<string> { "You do not have permission to perform this action" }
            ),

            // File not found or similar resource issues
            FileNotFoundException => (
                HttpStatusCode.NotFound,
                "Resource not found",
                new List<string> { "The requested resource could not be found" }
            ),

            // Timeout operations
            TimeoutException => (
                HttpStatusCode.RequestTimeout,
                "Request timeout",
                new List<string> { "The operation timed out. Please try again" }
            ),

            // Default for all other exceptions
            _ => (
                HttpStatusCode.InternalServerError,
                "Internal server error",
                new List<string> { "An unexpected error occurred. Please try again later" }
            )
        };
    }
}