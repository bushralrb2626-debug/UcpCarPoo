using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UcpCarPool.Models
{
    public class Ride
    {
        public int Id { get; set; }

        [Required]
        public string DriverId { get; set; } = string.Empty;

        [ForeignKey("DriverId")]
        public virtual ApplicationUser? Driver { get; set; }

        [Required]
        [Display(Name = "From")]
        public string FromLocation { get; set; } = string.Empty;

        [Required]
        [Display(Name = "To")]
        public string ToLocation { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime RideDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan DepartureTime { get; set; }

        // Original seats offered for this ride
        [Required]
        [Range(1, 8)]
        [Display(Name = "Total Seats")]
        public int TotalSeats { get; set; }

        // Live counter
        [Required]
        [Range(0, 8)]
        [Display(Name = "Available Seats")]
        public int AvailableSeats { get; set; }

        [Required]
        [Range(0, 5000)]
        public decimal Contribution { get; set; }

        public string? Notes { get; set; }

        // Active, Full, Completed, Cancelled
        public string Status { get; set; } = "Active";

        // Any, Male, Female
        public string? PreferredGender { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Existing ride requests
        public virtual ICollection<RideRequest> RideRequests { get; set; }
            = new List<RideRequest>();

        // NEW: Individual seats of this ride
        public virtual ICollection<RideSeat> RideSeats { get; set; }
            = new List<RideSeat>();
    }
}