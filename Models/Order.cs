using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models
{
    public enum OrderStatus
    {
        Pending,
        Preparing,
        Ready,
        Served,
        Completed,
        Cancelled
    }

    public enum PaymentMethod
    {
        Cash,
        Card,
        EWallet
    }

    public enum PaymentStatus
    {
        Pending,
        Paid
    }

    // Module: Ordering & Cart / Kitchen & Order Fulfillment
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }
        public User? Customer { get; set; }

        [ForeignKey(nameof(Table))]
        public int? TableId { get; set; }
        public DiningTable? Table { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // Subtotal before any discount/points/voucher is applied
        [Required]
        [Column(TypeName = "decimal(8,2)")]
        public decimal SubtotalAmount { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [StringLength(30)]
        public string? VoucherCodeUsed { get; set; }

        public int PointsRedeemed { get; set; } = 0;

        // Final payable amount = Subtotal - DiscountAmount - (points converted to RM)
        [Required]
        [Column(TypeName = "decimal(8,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [Required]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        // Points awarded to the customer once this order is Completed (1 point per RM1 spent)
        public int PointsEarned { get; set; } = 0;

        [StringLength(200)]
        public string? Remarks { get; set; }

        // Module: Kitchen Queue - internal staff note (separate from the customer's own remarks)
        [StringLength(300)]
        public string? StaffNote { get; set; }

        // Navigation
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
