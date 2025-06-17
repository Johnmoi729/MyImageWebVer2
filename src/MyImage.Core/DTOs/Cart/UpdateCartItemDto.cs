using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Cart;

/// <summary>
/// Data Transfer Object for updating existing cart items.
/// Allows customers to modify quantities or add/remove print sizes
/// without removing the entire photo from cart.
/// </summary>
public class UpdateCartItemDto
{
    /// <summary>
    /// Updated print selections for the cart item
    /// Replaces all existing selections for this photo
    /// Empty list effectively removes the photo from cart
    /// </summary>
    [Required(ErrorMessage = "Print selections are required")]
    public List<PrintSelectionDto> PrintSelections { get; set; } = new();
}