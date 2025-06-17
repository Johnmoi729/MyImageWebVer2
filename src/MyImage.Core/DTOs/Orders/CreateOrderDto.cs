using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Orders;

/// <summary>
/// Data Transfer Object for creating new orders.
/// This DTO implements Requirements 6 and 7 for payment processing and order creation.
/// Contains all information needed to convert a shopping cart into a confirmed order.
/// </summary>
public class CreateOrderDto
{
    /// <summary>
    /// Customer shipping address for order fulfillment
    /// Required for both credit card and branch payment methods
    /// Used for tax calculation and shipping
    /// </summary>
    [Required(ErrorMessage = "Shipping address is required")]
    public ShippingAddressDto ShippingAddress { get; set; } = new();

    /// <summary>
    /// Selected payment method for this order
    /// Values: "credit_card" or "branch_payment"
    /// Determines which payment information is processed
    /// </summary>
    [Required(ErrorMessage = "Payment method is required")]
    [RegularExpression("^(credit_card|branch_payment)$", ErrorMessage = "Invalid payment method")]
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Credit card information if credit_card payment method selected
    /// Contains encrypted payment data for secure processing
    /// Null if branch_payment method is chosen
    /// </summary>
    public CreditCardDto? CreditCard { get; set; }

    /// <summary>
    /// Branch payment information if branch_payment method selected
    /// Contains preferred branch location for in-person payment
    /// Null if credit_card method is chosen
    /// </summary>
    public BranchPaymentDto? BranchPayment { get; set; }
}