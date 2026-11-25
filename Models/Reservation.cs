using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingSystem.Models
{
    public enum ReservationStatus
    {
        Pending,
        Confirmed,
        Seated,
        Completed,
        Cancelled
    }

    // Module: Table & Reservation Maintenance
    public class Reservation
    {
        [Key]
        public int ReservationId { get; set; }

        [Required]
        [ForeignKey(nameof(Table))]
        public int TableId { get; set; }
        public DiningTable? Table { get; set; }

        [Required]
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }
        public User? Customer { get; set; }

        [Required, StringLength(100)]
        public string ContactName { get; set; } = string.Empty;

        [Required, Phone, StringLength(20)]
        public string ContactPhone { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime ReservationDateTime { get; set; }

        [Required, Range(1, 20)]
        public int PartySize { get; set; }

        [Required]
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        [StringLength(200)]
        public string? SpecialRequest { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
