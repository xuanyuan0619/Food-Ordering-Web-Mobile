using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    // Module: Points - redeemable reward catalog
    public class RewardItem
    {
        [Key]
        public int RewardItemId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [Required, Range(1, 100000)]
        public int PointsCost { get; set; }

        [StringLength(255)]
        public string? ImagePath { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
