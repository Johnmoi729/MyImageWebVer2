using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Orders;

/// <summary>
/// Data Transfer Object for credit card payment information.
/// This DTO implements Requirement 3: credit card encryption before server transmission.
/// All sensitive data must be encrypted client-side before reaching the server.
/// </summary>
public class CreditCardDto
{
    /// <summary>
    /// Encrypted card number using RSA public key encryption
    /// Original card number is encrypted in browser and never sent as plain text
    /// Server decrypts only for immediate payment processing
    /// </summary>
    [Required(ErrorMessage = "Encrypted card number is required")]
    public string EncryptedCardNumber { get; set; } = string.Empty;

    /// <summary>
    /// Cardholder name as it appears on the card
    /// Used for payment verification and billing
    /// Not encrypted as it's not considered sensitive data
    /// </summary>
    [Required(ErrorMessage = "Cardholder name is required")]
    [MaxLength(100, ErrorMessage = "Cardholder name cannot exceed 100 characters")]
    public string CardholderName { get; set; } = string.Empty;

    /// <summary>
    /// Card expiry month (01-12)
    /// Required for payment processing but not encrypted
    /// </summary>
    [Required(ErrorMessage = "Expiry month is required")]
    [RegularExpression(@"^(0[1-9]|1[0-2])$", ErrorMessage = "Expiry month must be 01-12")]
    public string ExpiryMonth { get; set; } = string.Empty;

    /// <summary>
    /// Card expiry year (4 digits)
    /// Required for payment processing but not encrypted
    /// </summary>
    [Required(ErrorMessage = "Expiry year is required")]
    [RegularExpression(@"^(20[2-9][0-9])$", ErrorMessage = "Please provide a valid expiry year")]
    public string ExpiryYear { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted CVV/CVC code using RSA public key encryption
    /// Most sensitive payment data - always encrypted before transmission
    /// Never stored permanently, only used for immediate processing
    /// </summary>
    [Required(ErrorMessage = "Encrypted CVV is required")]
    public string EncryptedCvv { get; set; } = string.Empty;
}