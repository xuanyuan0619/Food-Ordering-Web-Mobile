using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models
{
    public enum RedemptionStatus
    {
        Pending,
        Fulfilled
    }

    // Module: Points - redemption history
    public class Redemption
    {
        [Key]
        public int RedemptionId { get; set; }

        [Required]
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }
        public User? Customer { get; set; }

        [Required]
        [ForeignKey(nameof(RewardItem))]
        public int RewardItemId { get; set; }
        public RewardItem? RewardItem { get; set; }

        [Required]
        public int PointsSpent { get; set; }

        [Required]
        public RedemptionStatus Status { get; set; } = RedemptionStatus.Pending;

        public DateTime DateRedeemed { get; set; } = DateTime.Now;
    }
}
