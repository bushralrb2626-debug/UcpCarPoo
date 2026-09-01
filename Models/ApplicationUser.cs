using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace UcpCarPool.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        public string? UniversityId { get; set; }
        public string? Department { get; set; }
        public string? Batch { get; set; }
        public string? ProfileImage { get; set; }
        public string? Gender { get; set; }
        public string? EmergencyContact { get; set; }

        public bool IsUcpVerified { get; set; } = false;

        // Email OTP verification (demo mode — no real SMTP configured yet).
        // EmailConfirmed (inherited from IdentityUser) is the actual gate:
        // login is blocked until it's true.
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiresAt { get; set; }

        public double AverageRating { get; set; } = 0;
        public int TotalRatings { get; set; } = 0;
        public int TotalRides { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Vehicle? Vehicle { get; set; }
    }
}
