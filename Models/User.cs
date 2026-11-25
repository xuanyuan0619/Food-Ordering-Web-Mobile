using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Models
{
    public enum UserRole
    {
        Admin,
        Staff,
        Customer
    }

    public enum MembershipTier
    {
        Bronze,
        Silver,
        Gold
    }

    // Module: Security / Admin Maintenance
    [Index(nameof(Username), IsUnique = true)]
    [Index(nameof(Email), IsUnique = true)]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;

        // Store a salted hash, never the plain password.
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string PasswordSalt { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Phone, StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.Customer;

        public bool IsActive { get; set; } = true;

        // Additional feature: temporary login blocking after failed attempts
        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockoutEndUtc { get; set; }

        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Module: Membership & Points
        public int PointsBalance { get; set; } = 0;

        // Lifetime points earned (never decreases on redemption) - used to determine membership tier
        public int LifetimePoints { get; set; } = 0;

        [NotMapped]
        public MembershipTier Tier =>
            LifetimePoints >= 2000 ? MembershipTier.Gold :
            LifetimePoints >= 500 ? MembershipTier.Silver :
            MembershipTier.Bronze;

        [NotMapped]
        public bool IsLockedOut => LockoutEndUtc.HasValue && LockoutEndUtc.Value > DateTime.UtcNow;

        // Navigation
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
