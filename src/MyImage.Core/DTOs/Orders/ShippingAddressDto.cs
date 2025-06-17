using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Orders;

/// <summary>
/// Data Transfer Object for shipping address information.
/// Used in order creation and address validation.
/// </summary>
public class ShippingAddressDto
{
    /// <summary>
    /// Recipient's full name for shipping label
    /// May differ from account holder name for gift orders
    /// </summary>
    [Required(ErrorMessage = "Full name is required")]
    [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Primary street address line
    /// Required for all shipments
    /// </summary>
    [Required(ErrorMessage = "Street address is required")]
    [MaxLength(200, ErrorMessage = "Street address cannot exceed 200 characters")]
    public string StreetLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Secondary address line for apartment, suite, etc.
    /// Optional but important for accurate delivery
    /// </summary>
    [MaxLength(200, ErrorMessage = "Street address line 2 cannot exceed 200 characters")]
    public string StreetLine2 { get; set; } = string.Empty;

    /// <summary>
    /// City name for shipping and tax calculation
    /// Required for proper address validation
    /// </summary>
    [Required(ErrorMessage = "City is required")]
    [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State/province code for tax calculation
    /// Used to determine applicable tax rates
    /// Example: "MA", "NY", "CA"
    /// </summary>
    [Required(ErrorMessage = "State is required")]
    [MaxLength(10, ErrorMessage = "State cannot exceed 10 characters")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Postal/ZIP code for shipping and tax calculation
    /// Required for accurate delivery and tax determination
    /// </summary>
    [Required(ErrorMessage = "Postal code is required")]
    [MaxLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country code for international expansion
    /// Currently defaults to "USA" for domestic market
    /// </summary>
    [Required(ErrorMessage = "Country is required")]
    [MaxLength(10, ErrorMessage = "Country cannot exceed 10 characters")]
    public string Country { get; set; } = "USA";

    /// <summary>
    /// Contact phone number for delivery coordination
    /// Optional but recommended for shipping carrier contact
    /// </summary>
    [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    [RegularExpression(@"^[\+]?[1-9][\d]{0,15}$", ErrorMessage = "Please provide a valid phone number")]
    public string Phone { get; set; } = string.Empty;
}