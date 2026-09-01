using System.ComponentModel.DataAnnotations;

namespace UcpCarPool.ViewModels
{
    public class RateViewModel
    {
        [Required]
        public int RideId { get; set; }

        [Required]
        public string ToUserId { get; set; } = string.Empty;

        public string ToUserName { get; set; } = string.Empty;
        public string RouteLabel { get; set; } = string.Empty;

        [Required]
        [Range(1, 5, ErrorMessage = "Please select a star rating.")]
        public int Stars { get; set; }

        [StringLength(500)]
        [Display(Name = "Comment (optional)")]
        public string? Comment { get; set; }
    }
}
