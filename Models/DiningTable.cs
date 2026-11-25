using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public enum TableStatus
    {
        Available,
        Reserved,
        Occupied
    }

    // Module: Table & Reservation Maintenance
    public class DiningTable
    {
        [Key]
        public int TableId { get; set; }

        [Required, StringLength(10)]
        public string TableNumber { get; set; } = string.Empty;

        [Required, Range(1, 20)]
        public int Capacity { get; set; }

        [Required]
        public TableStatus Status { get; set; } = TableStatus.Available;

        // Navigation
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
