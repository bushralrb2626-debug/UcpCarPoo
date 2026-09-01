using System.ComponentModel.DataAnnotations;

namespace UcpCarPool.ViewModels
{
    public class VerifyOtpViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the 6-digit code.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits.")]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = string.Empty;
    }
}
