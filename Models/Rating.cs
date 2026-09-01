using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UcpCarPool.Models
{
    public class Rating
    {
        public int Id { get; set; }

        [Required]
        public int RideId { get; set; }

        [Required]
        public string FromUserId { get; set; } = string.Empty;

        [Required]
        public string ToUserId { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        public int Stars { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("RideId")]
        public virtual Ride? Ride { get; set; }

        [ForeignKey("FromUserId")]
        public virtual ApplicationUser? FromUser { get; set; }

        [ForeignKey("ToUserId")]
        public virtual ApplicationUser? ToUser { get; set; }
    }
}