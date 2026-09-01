using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UcpCarPool.Models
{
    public class RideRequest
    {
        public int Id { get; set; }

        [Required]
        public int RideId { get; set; }

        [Required]
        public string PassengerId { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public DateTime RequestDate { get; set; } = DateTime.Now;

        public string? Message { get; set; }

        [ForeignKey("RideId")]
        public virtual Ride? Ride { get; set; }

        [ForeignKey("PassengerId")]
        public virtual ApplicationUser? Passenger { get; set; }
    }
}