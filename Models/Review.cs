using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Models
{
    // Module: My Order - customer reviews (one review per ordered dish)
    [Index(nameof(OrderItemId), IsUnique = true)]
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        [ForeignKey(nameof(OrderItem))]
        public int OrderItemId { get; set; }
        public OrderItem? OrderItem { get; set; }

        // Denormalized for quick lookups / menu rating aggregation
        [Required]
        [ForeignKey(nameof(MenuItem))]
        public int MenuItemId { get; set; }
        public MenuItem? MenuItem { get; set; }

        [Required]
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }
        public User? Customer { get; set; }

        [Required, Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(300)]
        public string? Comment { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
