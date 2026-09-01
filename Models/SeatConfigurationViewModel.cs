using System.ComponentModel.DataAnnotations;

namespace UcpCarPool.ViewModels
{
    public class SeatConfigurationViewModel
    {
        public int RideId { get; set; }

        public string FromLocation { get; set; } = string.Empty;

        public string ToLocation { get; set; } = string.Empty;

        public DateTime RideDate { get; set; }

        public TimeSpan DepartureTime { get; set; }

        public List<SeatConfigurationItem> Seats { get; set; }
            = new List<SeatConfigurationItem>();
    }

    public class SeatConfigurationItem
    {
        public int Id { get; set; }

        [Required]
        public string SeatNumber { get; set; } = string.Empty;

        [Required]
        public string SeatType { get; set; } = "General";

        public string Status { get; set; } = "Available";

        public string? BookedByUserId { get; set; }

        // Used by UI to determine whether this seat
        // can still be configured.
        public bool IsBooked =>
            Status.Equals(
                "Booked",
                StringComparison.OrdinalIgnoreCase);
    }
}