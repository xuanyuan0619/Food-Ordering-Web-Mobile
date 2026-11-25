using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Models
{
    public enum DiscountType
    {
        Percentage,
        FixedAmount
    }

    // Module: Discount & Voucher (applied at Checkout)
    [Index(nameof(Code), IsUnique = true)]
    public class VoucherCode
    {
        [Key]
        public int VoucherCodeId { get; set; }

        [Required, StringLength(30)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;

        [Required]
        [Column(TypeName = "decimal(8,2)")]
        [Range(0.01, 100)]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal MinSpend { get; set; } = 0;

        [Required]
        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
