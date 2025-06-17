using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Admin;

/// <summary>
/// Data Transfer Object for admin dashboard statistics.
/// Provides quick overview of system status and pending work.
/// </summary>
public class AdminDashboardDto
{
    /// <summary>
    /// Number of orders awaiting payment verification
    /// Critical metric for daily admin workflow
    /// </summary>
    public int PendingOrders { get; set; }

    /// <summary>
    /// Number of orders currently being processed/printed
    /// Indicates current workload in fulfillment pipeline
    /// </summary>
    public int ProcessingOrders { get; set; }

    /// <summary>
    /// Number of orders completed today
    /// Performance metric for daily productivity
    /// </summary>
    public int CompletedToday { get; set; }

    /// <summary>
    /// Total revenue for current period
    /// Financial summary for business tracking
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Number of active users in the system
    /// Growth metric for business analysis
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// Total storage used by photos in bytes
    /// Infrastructure monitoring for capacity planning
    /// </summary>
    public long StorageUsed { get; set; }
}