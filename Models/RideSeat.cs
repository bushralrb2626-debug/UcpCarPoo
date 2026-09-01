using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UcpCarPool.Models
{
    public class RideSeat
    {
        public int Id { get; set; }

        [Required]
        public int RideId { get; set; }

        [ForeignKey("RideId")]
        public virtual Ride? Ride { get; set; }

        [Required]
        [StringLength(10)]
        public string SeatNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string SeatType { get; set; } = "General";

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Available";

        public string? BookedByUserId { get; set; }

        [ForeignKey("BookedByUserId")]
        public virtual ApplicationUser? BookedByUser { get; set; }
    }
}