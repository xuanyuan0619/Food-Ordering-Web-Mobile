using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.ViewModels
{
    // Module: Menu Maintenance (admin create/edit form)
    public class MenuItemViewModel
    {
        public int MenuItemId { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Item Name")]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, 9999.99, ErrorMessage = "Price must be between RM0.01 and RM9999.99")]
        public decimal Price { get; set; }

        // Free-text category name. If it doesn't already exist, the controller creates it on the fly.
        // No [Required] here - controller validates manually so the error message points to the right field.
        [StringLength(50)]
        [Display(Name = "Category")]
        public string CategoryName { get; set; } = string.Empty;

        public bool IsAvailable { get; set; } = true;

        public bool IsSignatureDish { get; set; }

        // Multiple photo upload
        [Display(Name = "Photos")]
        public List<IFormFile> ImageFiles { get; set; } = new();

        public List<ExistingImageViewModel> ExistingImages { get; set; } = new();
    }

    public class ExistingImageViewModel
    {
        public int MenuItemImageId { get; set; }
        public string ImagePath { get; set; } = string.Empty;
    }
}