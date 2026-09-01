using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UcpCarPool.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required]
        public string DriverId { get; set; } = string.Empty;

        [ForeignKey("DriverId")]
        public virtual ApplicationUser? Driver { get; set; }

        [Required]
        [Display(Name = "Vehicle Type")]
        public string VehicleType { get; set; } = string.Empty;

        [Required]
        public string Model { get; set; } = string.Empty;

        public string? Color { get; set; }

        [Required]
        [Display(Name = "Plate Number")]
        public string PlateNumber { get; set; } = string.Empty;

        [Required]
        [Range(1, 8)]
        [Display(Name = "Total Seats")]
        public int TotalSeats { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
