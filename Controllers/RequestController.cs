using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UcpCarPool.Data;
using UcpCarPool.Models;

namespace UcpCarPool.Controllers
{
    [Authorize]
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RequestsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // VIEW REQUESTS FOR A RIDE
        // =========================================================

        public async Task<IActionResult> ForRide(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var ride = await _context.Rides
                .Include(r => r.RideRequests)
                    .ThenInclude(rr => rr.Passenger)
                .Include(r => r.RideSeats)
                .FirstOrDefaultAsync(
                    r => r.Id == id &&
                         r.DriverId == user.Id);

            if (ride == null)
                return NotFound();

            return View(ride);
        }


        // =========================================================
        // ACCEPT REQUEST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();


            // -----------------------------------------------------
            // Get request + ride
            // -----------------------------------------------------

            var request = await _context.RideRequests
                .Include(rr => rr.Ride)
                .FirstOrDefaultAsync(
                    rr => rr.Id == id);


            if (request == null || request.Ride == null)
                return NotFound();


            var ride = request.Ride;


            // -----------------------------------------------------
            // Security:
            // only ride owner can accept request
            // -----------------------------------------------------

            if (ride.DriverId != user.Id)
                return Forbid();


            // -----------------------------------------------------
            // Request must still be pending
            // -----------------------------------------------------

            if (request.Status != "Pending")
            {
                TempData["Info"] =
                    "This request has already been actioned.";

                return RedirectToAction(
                    nameof(ForRide),
                    new { id = ride.Id });
            }


            // -----------------------------------------------------
            // Ride must still be active
            // -----------------------------------------------------

            if (ride.Status != "Active")
            {
                TempData["Error"] =
                    "This ride is no longer accepting requests.";

                return RedirectToAction(
                    nameof(ForRide),
                    new { id = ride.Id });
            }


            // -----------------------------------------------------
            // Check available seats
            // -----------------------------------------------------

            if (ride.AvailableSeats <= 0)
            {
                TempData["Error"] =
                    "No seats left on this ride.";

                return RedirectToAction(
                    nameof(ForRide),
                    new { id = ride.Id });
            }


            // -----------------------------------------------------
            // Find an available RideSeat
            // -----------------------------------------------------

            var availableSeat = await _context.RideSeats
                .Where(s =>
                    s.RideId == ride.Id &&
                    s.Status == "Available")
                .OrderBy(s => s.SeatNumber)
                .FirstOrDefaultAsync();


            if (availableSeat == null)
            {
                TempData["Error"] =
                    "No available seat record was found for this ride.";

                return RedirectToAction(
                    nameof(ForRide),
                    new { id = ride.Id });
            }


            // =====================================================
            // ACCEPT REQUEST
            // =====================================================

            request.Status = "Accepted";


            // -----------------------------------------------------
            // Book the specific seat
            // -----------------------------------------------------

            availableSeat.Status = "Booked";
            availableSeat.BookedByUserId = request.PassengerId;


            // -----------------------------------------------------
            // Update ride seats
            // -----------------------------------------------------

            ride.AvailableSeats -= 1;


            // -----------------------------------------------------
            // If no seats remain, mark ride Full
            // -----------------------------------------------------

            if (ride.AvailableSeats <= 0)
            {
                ride.AvailableSeats = 0;
                ride.Status = "Full";
            }


            // =====================================================
            // NOTIFICATION TO PASSENGER
            // =====================================================

            _context.Notifications.Add(
                new Notification
                {
                    UserId = request.PassengerId,

                    Title = "Ride Request Accepted",

                    Message =
                        $"Your request for the ride " +
                        $"{ride.FromLocation} → {ride.ToLocation} " +
                        $"on {ride.RideDate:d MMM yyyy} was accepted. " +
                        $"Seat {availableSeat.SeatNumber} has been assigned to you.",

                    IsRead = false,

                    CreatedAt = DateTime.Now
                });


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Request accepted. Seat {availableSeat.SeatNumber} assigned.";


            return RedirectToAction(
                nameof(ForRide),
                new { id = ride.Id });
        }


        // =========================================================
        // REJECT REQUEST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();


            var request = await _context.RideRequests
                .Include(rr => rr.Ride)
                .FirstOrDefaultAsync(
                    rr => rr.Id == id);


            if (request == null || request.Ride == null)
                return NotFound();


            var ride = request.Ride;


            // -----------------------------------------------------
            // Security
            // -----------------------------------------------------

            if (ride.DriverId != user.Id)
                return Forbid();


            // -----------------------------------------------------
            // Request must be pending
            // -----------------------------------------------------

            if (request.Status != "Pending")
            {
                TempData["Info"] =
                    "This request has already been actioned.";

                return RedirectToAction(
                    nameof(ForRide),
                    new { id = ride.Id });
            }


            // -----------------------------------------------------
            // Reject request
            // -----------------------------------------------------

            request.Status = "Rejected";


            // =====================================================
            // NOTIFICATION
            // =====================================================

            _context.Notifications.Add(
                new Notification
                {
                    UserId = request.PassengerId,

                    Title = "Ride Request Declined",

                    Message =
                        $"Your request for the ride " +
                        $"{ride.FromLocation} → {ride.ToLocation} " +
                        $"on {ride.RideDate:d MMM yyyy} was declined.",

                    IsRead = false,

                    CreatedAt = DateTime.Now
                });


            await _context.SaveChangesAsync();


            TempData["Info"] =
                "Request rejected.";


            return RedirectToAction(
                nameof(ForRide),
                new { id = ride.Id });
        }


        // =========================================================
        // MY REQUESTS
        // =========================================================

        public async Task<IActionResult> MyRequests()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();


            var requests = await _context.RideRequests
                .Include(rr => rr.Ride)
                    .ThenInclude(r => r!.Driver)
                .Where(
                    rr => rr.PassengerId == user.Id)
                .OrderByDescending(
                    rr => rr.RequestDate)
                .ToListAsync();


            return View(requests);
        }


        // =========================================================
        // CANCEL REQUEST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();


            var request = await _context.RideRequests
                .Include(rr => rr.Ride)
                .FirstOrDefaultAsync(
                    rr =>
                        rr.Id == id &&
                        rr.PassengerId == user.Id);


            if (request == null)
                return NotFound();


            var ride = request.Ride;


            // -----------------------------------------------------
            // Don't allow cancellation after already cancelled
            // or rejected
            // -----------------------------------------------------

            if (request.Status == "Cancelled")
            {
                TempData["Info"] =
                    "This request is already cancelled.";

                return RedirectToAction(
                    nameof(MyRequests));
            }


            if (request.Status == "Rejected")
            {
                TempData["Info"] =
                    "A rejected request cannot be cancelled.";

                return RedirectToAction(
                    nameof(MyRequests));
            }


            // =====================================================
            // IF ACCEPTED
            // =====================================================

            if (request.Status == "Accepted" &&
                ride != null)
            {
                // -------------------------------------------------
                // Find the seat booked by this passenger
                // -------------------------------------------------

                var bookedSeat = await _context.RideSeats
                    .FirstOrDefaultAsync(
                        s =>
                            s.RideId == ride.Id &&
                            s.BookedByUserId == request.PassengerId &&
                            s.Status == "Booked");


                // -------------------------------------------------
                // Release the seat
                // -------------------------------------------------

                if (bookedSeat != null)
                {
                    bookedSeat.Status = "Available";
                    bookedSeat.BookedByUserId = null;
                }


                // -------------------------------------------------
                // Increase available seats
                // -------------------------------------------------

                ride.AvailableSeats += 1;


                if (ride.AvailableSeats > ride.TotalSeats)
                    ride.AvailableSeats = ride.TotalSeats;


                // -------------------------------------------------
                // If ride was Full, make it Active again
                // -------------------------------------------------

                if (ride.Status == "Full")
                    ride.Status = "Active";


                // =================================================
                // NOTIFY DRIVER
                // =================================================

                _context.Notifications.Add(
                    new Notification
                    {
                        UserId = ride.DriverId,

                        Title = "Passenger Cancelled",

                        Message =
                            $"A passenger cancelled their confirmed seat " +
                            $"on your ride {ride.FromLocation} → " +
                            $"{ride.ToLocation} on " +
                            $"{ride.RideDate:d MMM yyyy}.",

                        IsRead = false,

                        CreatedAt = DateTime.Now
                    });
            }


            // -----------------------------------------------------
            // Cancel request
            // -----------------------------------------------------

            request.Status = "Cancelled";


            await _context.SaveChangesAsync();


            TempData["Info"] =
                "Request cancelled.";


            return RedirectToAction(
                nameof(MyRequests));
        }
    }
}