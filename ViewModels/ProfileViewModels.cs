using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.ViewModels
{
    // Module: Profile
    public class ProfileViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // Module: My Order - five-star review
    public class ReviewViewModel
    {
        public int OrderItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;

        [Required, Range(1, 5, ErrorMessage = "Please choose a rating between 1 and 5 stars")]
        public int Rating { get; set; }

        [StringLength(300)]
        public string? Comment { get; set; }
    }
}
