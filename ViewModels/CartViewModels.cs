using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.ViewModels
{
    // Module: Ordering & Cart
    public class CartItemViewModel
    {
        public int MenuItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }

        [Range(1, 50, ErrorMessage = "Quantity must be between 1 and 50")]
        public int Quantity { get; set; }

        public string? SpecialInstructions { get; set; }

        public decimal Subtotal => UnitPrice * Quantity;
    }

    public class CheckoutViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();

        [Required(ErrorMessage = "Please select a table")]
        [Display(Name = "Dining Table")]
        public int TableId { get; set; }

        [StringLength(200)]
        public string? Remarks { get; set; }

        [Display(Name = "Voucher Code")]
        [StringLength(30)]
        public string? VoucherCode { get; set; }

        [Display(Name = "Use Points")]
        public bool UsePoints { get; set; }

        [Range(0, 1000000)]
        [Display(Name = "Points to Redeem")]
        public int PointsToRedeem { get; set; }

        [Required(ErrorMessage = "Please select a payment method")]
        [Display(Name = "Payment Method")]
        public FoodOrderingSystem.Models.PaymentMethod PaymentMethod { get; set; } = FoodOrderingSystem.Models.PaymentMethod.Cash;

        public decimal Subtotal => Items.Sum(i => i.Subtotal);

        // Populated by the controller after validating the voucher/points, used to redisplay on validation failure
        public decimal DiscountPreview { get; set; }
        public int AvailablePoints { get; set; }
    }
}
