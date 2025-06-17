using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Common;

/// <summary>
/// Standard API response wrapper for consistent response format.
/// Used across all endpoints to provide uniform structure.
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The actual data payload
    /// Null if operation failed or no data to return
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Human-readable success or informational message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Collection of error messages if operation failed
    /// Empty array for successful operations
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Timestamp when response was generated
    /// Useful for debugging and audit logs
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Create successful response with data
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// Create error response with messages
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }

    /// <summary>
    /// Create error response with single error
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string message, string error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = new List<string> { error }
        };
    }
}