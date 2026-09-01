using System.ComponentModel.DataAnnotations;

namespace UcpCarPool.ViewModels
{
    public class RideSeatViewModel
    {
        public int Id { get; set; }

        public int RideId { get; set; }

        [Display(Name = "Seat Number")]
        public string SeatNumber { get; set; } = string.Empty;

        public string SeatType { get; set; } = "General";

        public string Status { get; set; } = "Available";

        public string? BookedByUserId { get; set; }

        public string? BookedByUserName { get; set; }

        public bool IsAvailable =>
            Status.Equals("Available", StringComparison.OrdinalIgnoreCase);

        public bool IsBooked =>
            Status.Equals("Booked", StringComparison.OrdinalIgnoreCase);
    }
}