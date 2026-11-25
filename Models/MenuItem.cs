using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models
{
    // Module: Menu Maintenance
    public class MenuItem
    {
        [Key]
        public int MenuItemId { get; set; }

        [Required, StringLength(100)]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(8,2)")]
        [Range(0.01, 9999.99, ErrorMessage = "Price must be between 0.01 and 9999.99")]
        public decimal Price { get; set; }

        [StringLength(255)]
        public string? ImagePath { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Additional feature hook: e.g. spicy level, calories, etc. Keep minimal per assignment note.
        public bool IsSignatureDish { get; set; } = false;

        [Required]
        [ForeignKey(nameof(Category))]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // Navigation
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<MenuItemImage> Images { get; set; } = new List<MenuItemImage>();
    }
}
