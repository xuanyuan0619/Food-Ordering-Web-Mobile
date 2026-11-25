using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models
{
    // Module: Ordering & Cart
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        [Required]
        [ForeignKey(nameof(Order))]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required]
        [ForeignKey(nameof(MenuItem))]
        public int MenuItemId { get; set; }
        public MenuItem? MenuItem { get; set; }

        [Required, Range(1, 50)]
        public int Quantity { get; set; }

        // Price captured at order time so later menu price changes don't rewrite history.
        [Required]
        [Column(TypeName = "decimal(8,2)")]
        public decimal UnitPrice { get; set; }

        [StringLength(150)]
        public string? SpecialInstructions { get; set; }

        [NotMapped]
        public decimal Subtotal => Quantity * UnitPrice;
    }
}
