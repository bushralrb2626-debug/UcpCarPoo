using System.ComponentModel.DataAnnotations;

namespace UcpCarPool.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [Display(Name = "Full Name")]
        [StringLength(100, MinimumLength = 3)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "University ID")]
        public string UniversityId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your batch.")]
        [Display(Name = "Batch")]
        public string Batch { get; set; } = string.Empty;

        public string? Department { get; set; }

        [Required(ErrorMessage = "Please select your gender.")]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
