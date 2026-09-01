using System.ComponentModel.DataAnnotations;

namespace UcpCarPool.ViewModels
{
    public class RideFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Pickup location is required.")]
        [Display(Name = "From")]
        public string FromLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination is required.")]
        [Display(Name = "To")]
        public string ToLocation { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Ride Date")]
        public DateTime RideDate { get; set; } = DateTime.Today.AddDays(1);

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Departure Time")]
        public TimeSpan DepartureTime { get; set; }

        [Required]
        [Range(1, 8, ErrorMessage = "Seats must be between 1 and 8.")]
        [Display(Name = "Seats Offered")]
        public int TotalSeats { get; set; } = 1;

        [Required]
        [Range(0, 5000, ErrorMessage = "Enter a valid contribution amount.")]
        [Display(Name = "Contribution per Seat (Rs.)")]
        public decimal Contribution { get; set; }

        [Display(Name = "Preferred Passenger Gender")]
        public string? PreferredGender { get; set; } // "Any", "Male", "Female"

        [Display(Name = "Notes for passengers")]
        [StringLength(300)]
        public string? Notes { get; set; }
    }

    public class RideSearchViewModel
    {
        [Display(Name = "From")]
        public string? FromLocation { get; set; }

        [Display(Name = "To")]
        public string? ToLocation { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime? RideDate { get; set; }

        public List<RideResultViewModel> Results { get; set; } = new();
    }

    public class RideResultViewModel
    {
        public int Id { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public double DriverRating { get; set; }
        public bool DriverVerified { get; set; }
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public DateTime RideDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public int AvailableSeats { get; set; }
        public decimal Contribution { get; set; }
        public string? PreferredGender { get; set; }
        public string VehicleModel { get; set; } = string.Empty;

        public int MatchScore { get; set; }
        public bool AlreadyRequested { get; set; }

        // True when this ride is gender-restricted and doesn't match the
        // searching user's gender — used to grey it out / block requesting.
        public bool GenderBlocked { get; set; }
    }
}
