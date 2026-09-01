using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UcpCarPool.Models
{
    public class Report
    {
        public int Id { get; set; }

        [Required]
        public string ReporterId { get; set; } = string.Empty;

        [Required]
        public string ReportedUserId { get; set; } = string.Empty;

        public int? RideId { get; set; }

        [Required]
        [StringLength(200)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public string Status { get; set; } = "Pending";  // Pending, Reviewed, Resolved

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("ReporterId")]
        public virtual ApplicationUser? Reporter { get; set; }

        [ForeignKey("ReportedUserId")]
        public virtual ApplicationUser? ReportedUser { get; set; }

        [ForeignKey("RideId")]
        public virtual Ride? Ride { get; set; }
    }
}