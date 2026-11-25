using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.ViewModels
{
    // Module: Table & Reservation Maintenance
    public class ReservationViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Contact Name")]
        public string ContactName { get; set; } = string.Empty;

        [Required, Phone, StringLength(20)]
        [Display(Name = "Contact Phone")]
        public string ContactPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a table")]
        [Display(Name = "Table")]
        public int TableId { get; set; }

        [Required, Range(1, 20, ErrorMessage = "Party size must be between 1 and 20")]
        [Display(Name = "Party Size")]
        public int PartySize { get; set; }

        [Required]
        [Display(Name = "Reservation Date & Time")]
        [DataType(DataType.DateTime)]
        public DateTime ReservationDateTime { get; set; } = DateTime.Now.AddHours(1);

        [StringLength(200)]
        [Display(Name = "Special Request")]
        public string? SpecialRequest { get; set; }
    }
}
