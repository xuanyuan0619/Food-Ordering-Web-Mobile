using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models
{
    // Module: Menu Maintenance - multiple photos per dish
    public class MenuItemImage
    {
        [Key]
        public int MenuItemImageId { get; set; }

        [Required]
        [ForeignKey(nameof(MenuItem))]
        public int MenuItemId { get; set; }
        public MenuItem? MenuItem { get; set; }

        [Required, StringLength(255)]
        public string ImagePath { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
    }
}
