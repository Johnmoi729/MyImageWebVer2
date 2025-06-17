using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Cart;

/// <summary>
/// Data Transfer Object for adding photos to shopping cart.
/// This DTO implements Requirement 2: "user should be able to mark the photographs 
/// he wants to order and specify the size of the print and the number of copies"
/// 
/// The key innovation here is supporting multiple print sizes per photo in a single request,
/// which matches how customers actually shop for prints.
/// </summary>
public class AddToCartDto
{
    /// <summary>
    /// Photo identifier that the user wants to add to their cart
    /// Must be a valid photo ID that belongs to the authenticated user
    /// </summary>
    [Required(ErrorMessage = "Photo ID is required")]
    public string PhotoId { get; set; } = string.Empty;

    /// <summary>
    /// Collection of print selections for this photo
    /// Allows ordering multiple sizes and quantities in one cart action
    /// Example: 10×4×6 prints + 2×5×7 prints + 1×8×10 print of the same photo
    /// </summary>
    [Required(ErrorMessage = "At least one print selection is required")]
    [MinLength(1, ErrorMessage = "At least one print selection must be specified")]
    public List<PrintSelectionDto> PrintSelections { get; set; } = new();
}