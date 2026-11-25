using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Models
{
    // Module: Wishlist (Customer favourites)
    [Index(nameof(CustomerId), nameof(MenuItemId), IsUnique = true)]
    public class WishlistItem
    {
        [Key]
        public int WishlistItemId { get; set; }

        [Required]
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }
        public User? Customer { get; set; }

        [Required]
        [ForeignKey(nameof(MenuItem))]
        public int MenuItemId { get; set; }
        public MenuItem? MenuItem { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.Now;
    }
}
