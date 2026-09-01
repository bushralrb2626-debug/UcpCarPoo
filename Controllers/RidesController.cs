using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UcpCarPool.Data;
using UcpCarPool.Models;
using UcpCarPool.ViewModels;

namespace UcpCarPool.Controllers
{
    [Authorize]
    public class RidesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RidesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // MY RIDES
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var rides = await _context.Rides
                .Where(r => r.DriverId == user.Id)
                .OrderByDescending(r => r.RideDate)
                .ThenByDescending(r => r.CreatedAt)
                .Include(r => r.RideRequests)
                .Include(r => r.RideSeats)
                .ToListAsync();

            return View(rides);
        }

        // =========================================================
        // CREATE RIDE - GET
        // =========================================================

        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.DriverId == user.Id);

            if (vehicle == null)
            {
                TempData["Error"] =
                    "You need to register a vehicle before posting a ride.";

                return RedirectToAction("Create", "Vehicles");
            }

            ViewBag.MaxSeats = vehicle.TotalSeats;

            return View(new RideFormViewModel
            {
                TotalSeats = Math.Min(1, vehicle.TotalSeats)
            });
        }

        // =========================================================
        // CREATE RIDE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RideFormViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.DriverId == user.Id);

            if (vehicle == null)
            {
                TempData["Error"] =
                    "You need to register a vehicle before posting a ride.";

                return RedirectToAction("Create", "Vehicles");
            }

            if (model.RideDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.RideDate),
                    "Ride date can't be in the past.");
            }

            if (model.TotalSeats > vehicle.TotalSeats)
            {
                ModelState.AddModelError(
                    nameof(model.TotalSeats),
                    $"Your vehicle only has {vehicle.TotalSeats} seats.");
            }

            if (model.TotalSeats < 1)
            {
                ModelState.AddModelError(
                    nameof(model.TotalSeats),
                    "At least 1 seat must be offered.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.MaxSeats = vehicle.TotalSeats;
                return View(model);
            }

            var ride = new Ride
            {
                DriverId = user.Id,
                FromLocation = model.FromLocation.Trim(),
                ToLocation = model.ToLocation.Trim(),
                RideDate = model.RideDate.Date,
                DepartureTime = model.DepartureTime,
                TotalSeats = model.TotalSeats,
                AvailableSeats = model.TotalSeats,
                Contribution = model.Contribution,
                PreferredGender =
                    string.IsNullOrWhiteSpace(model.PreferredGender)
                        ? "Any"
                        : model.PreferredGender,
                Notes = model.Notes,
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            _context.Rides.Add(ride);

            await _context.SaveChangesAsync();

            // =====================================================
            // CREATE SEATS
            // =====================================================

            var seats = new List<RideSeat>();

            for (int i = 1; i <= ride.TotalSeats; i++)
            {
                seats.Add(new RideSeat
                {
                    RideId = ride.Id,
                    SeatNumber = i.ToString(),
                    SeatType = "General",
                    Status = "Available",
                    BookedByUserId = null
                });
            }

            _context.RideSeats.AddRange(seats);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Ride posted successfully with {ride.TotalSeats} seats.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT RIDE - GET
        // =========================================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var ride = await _context.Rides
                .Include(r => r.RideSeats)
                .FirstOrDefaultAsync(
                    r => r.Id == id && r.DriverId == user.Id);

            if (ride == null)
                return NotFound();

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.DriverId == user.Id);

            ViewBag.MaxSeats =
                vehicle?.TotalSeats ?? ride.TotalSeats;

            return View(new RideFormViewModel
            {
                Id = ride.Id,
                FromLocation = ride.FromLocation,
                ToLocation = ride.ToLocation,
                RideDate = ride.RideDate,
                DepartureTime = ride.DepartureTime,
                TotalSeats = ride.TotalSeats,
                Contribution = ride.Contribution,
                PreferredGender = ride.PreferredGender,
                Notes = ride.Notes
            });
        }

        // =========================================================
        // EDIT RIDE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            RideFormViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var ride = await _context.Rides
                .Include(r => r.RideSeats)
                .FirstOrDefaultAsync(
                    r => r.Id == id && r.DriverId == user.Id);

            if (ride == null)
                return NotFound();

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.DriverId == user.Id);

            var maxSeats =
                vehicle?.TotalSeats ?? model.TotalSeats;

            ViewBag.MaxSeats = maxSeats;

            var seatsBooked =
                ride.TotalSeats - ride.AvailableSeats;

            if (model.TotalSeats > maxSeats)
            {
                ModelState.AddModelError(
                    nameof(model.TotalSeats),
                    $"Your vehicle only has {maxSeats} seats.");
            }

            if (model.TotalSeats < seatsBooked)
            {
                ModelState.AddModelError(
                    nameof(model.TotalSeats),
                    $"You already have {seatsBooked} seat(s) booked.");
            }

            if (model.RideDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.RideDate),
                    "Ride date can't be in the past.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var oldTotalSeats = ride.TotalSeats;

            ride.FromLocation = model.FromLocation.Trim();
            ride.ToLocation = model.ToLocation.Trim();
            ride.RideDate = model.RideDate.Date;
            ride.DepartureTime = model.DepartureTime;

            ride.AvailableSeats +=
                model.TotalSeats - oldTotalSeats;

            ride.TotalSeats = model.TotalSeats;

            ride.Contribution = model.Contribution;

            ride.PreferredGender =
                string.IsNullOrWhiteSpace(model.PreferredGender)
                    ? "Any"
                    : model.PreferredGender;

            ride.Notes = model.Notes;

            // Add new seats
            if (model.TotalSeats > oldTotalSeats)
            {
                for (
                    int i = oldTotalSeats + 1;
                    i <= model.TotalSeats;
                    i++)
                {
                    _context.RideSeats.Add(new RideSeat
                    {
                        RideId = ride.Id,
                        SeatNumber = i.ToString(),
                        SeatType = "General",
                        Status = "Available",
                        BookedByUserId = null
                    });
                }
            }

            // Remove only available extra seats
            if (model.TotalSeats < oldTotalSeats)
            {
                var seatsToRemove = ride.RideSeats
                    .Where(s =>
                        int.TryParse(s.SeatNumber, out var number)
                        && number > model.TotalSeats
                        && s.Status == "Available")
                    .ToList();

                _context.RideSeats.RemoveRange(seatsToRemove);
            }

            if (ride.AvailableSeats == 0)
                ride.Status = "Full";
            else if (ride.Status == "Full")
                ride.Status = "Active";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Ride updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE - GET
        // =========================================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var ride = await _context.Rides
                .Include(r => r.RideRequests)
                .FirstOrDefaultAsync(
                    r => r.Id == id && r.DriverId == user.Id);

            if (ride == null)
                return NotFound();

            return View(ride);
        }

        // =========================================================
        // DELETE - POST
        // =========================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var ride = await _context.Rides
                .Include(r => r.RideRequests)
                .FirstOrDefaultAsync(
                    r => r.Id == id && r.DriverId == user.Id);

            if (ride == null)
                return NotFound();

            var hasAcceptedRequests =
                ride.RideRequests.Any(
                    rr => rr.Status == "Accepted");

            if (hasAcceptedRequests)
            {
                ride.Status = "Cancelled";

                await _context.SaveChangesAsync();

                TempData["Info"] =
                    "This ride had confirmed passengers, so it was cancelled instead of deleted.";
            }
            else
            {
                _context.Rides.Remove(ride);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Ride deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // MARK COMPLETED
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCompleted(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var ride = await _context.Rides
                .FirstOrDefaultAsync(
                    r => r.Id == id && r.DriverId == user.Id);

            if (ride == null)
                return NotFound();

            if (ride.Status != "Active"
                && ride.Status != "Full")
            {
                TempData["Error"] =
                    "Only active rides can be marked completed.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            ride.Status = "Completed";

            user.TotalRides += 1;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Ride marked as completed. You and your passengers can now rate each other.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // DETAILS
        // =========================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var ride = await _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.RideRequests)
                    .ThenInclude(rr => rr.Passenger)
                .Include(r => r.RideSeats)
                    .ThenInclude(s => s.BookedByUser)
                .FirstOrDefaultAsync(
                    r => r.Id == id);

            if (ride == null)
                return NotFound();

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(
                    v => v.DriverId == ride.DriverId);

            ViewBag.Vehicle = vehicle;

            var isOwner =
                ride.DriverId == user.Id;

            var myRequest =
                ride.RideRequests
                    .FirstOrDefault(
                        rr => rr.PassengerId == user.Id);

            var genderBlocked =
                !isOwner
                && ride.PreferredGender != "Any"
                && !string.IsNullOrEmpty(
                    ride.PreferredGender)
                && user.Gender != ride.PreferredGender;

            ViewBag.IsOwner = isOwner;

            ViewBag.MyRequestStatus =
                myRequest?.Status;

            ViewBag.GenderBlocked =
                genderBlocked;

            ViewBag.CanRequest =
                !isOwner
                && myRequest == null
                && !genderBlocked
                && ride.Status == "Active"
                && ride.AvailableSeats > 0
                && ride.RideDate.Date >= DateTime.Today;

            if (!isOwner
                && myRequest?.Status == "Accepted"
                && ride.Status == "Completed")
            {
                ViewBag.AlreadyRatedDriver =
                    await _context.Ratings.AnyAsync(
                        r =>
                            r.RideId == ride.Id
                            && r.FromUserId == user.Id
                            && r.ToUserId == ride.DriverId);
            }
            else
            {
                ViewBag.AlreadyRatedDriver = false;
            }

            return View(ride);
        }

        // =========================================================
        // SEARCH + SMART MATCHING
        // =========================================================

        public async Task<IActionResult> Search(
            RideSearchViewModel model)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var rides = await _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.RideRequests)
                .Include(r => r.RideSeats)
                .Where(
                    r =>
                        r.Status == "Active"
                        && r.AvailableSeats > 0
                        && r.RideDate.Date >= DateTime.Today
                        && r.DriverId != user.Id)
                .ToListAsync();

            var vehicles =
                await _context.Vehicles
                    .ToDictionaryAsync(
                        v => v.DriverId,
                        v => v);

            var hasSearchCriteria =
                !string.IsNullOrWhiteSpace(
                    model.FromLocation)
                || !string.IsNullOrWhiteSpace(
                    model.ToLocation)
                || model.RideDate.HasValue;

            var results =
                new List<RideResultViewModel>();

            foreach (var ride in rides)
            {
                int score = 0;

                // FROM
                if (!string.IsNullOrWhiteSpace(
                    model.FromLocation))
                {
                    if (ride.FromLocation.Contains(
                        model.FromLocation,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        score += 35;
                    }
                }
                else
                {
                    score += 15;
                }

                // TO
                if (!string.IsNullOrWhiteSpace(
                    model.ToLocation))
                {
                    if (ride.ToLocation.Contains(
                        model.ToLocation,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        score += 35;
                    }
                }
                else
                {
                    score += 15;
                }

                // DATE
                if (model.RideDate.HasValue)
                {
                    if (ride.RideDate.Date ==
                        model.RideDate.Value.Date)
                    {
                        score += 15;
                    }
                }
                else
                {
                    score += 7;
                }

                // SEATS
                score += Math.Min(
                    ride.AvailableSeats * 2,
                    8);

                // RATING
                score += (int)Math.Round(
                    ride.Driver!.AverageRating);

                // VERIFIED
                if (ride.Driver.IsUcpVerified)
                    score += 5;

                // HARD FILTER
                if (hasSearchCriteria)
                {
                    var fromMatches =
                        string.IsNullOrWhiteSpace(
                            model.FromLocation)
                        || ride.FromLocation.Contains(
                            model.FromLocation,
                            StringComparison.OrdinalIgnoreCase);

                    var toMatches =
                        string.IsNullOrWhiteSpace(
                            model.ToLocation)
                        || ride.ToLocation.Contains(
                            model.ToLocation,
                            StringComparison.OrdinalIgnoreCase);

                    var dateMatches =
                        !model.RideDate.HasValue
                        || ride.RideDate.Date ==
                           model.RideDate.Value.Date;

                    if (!fromMatches
                        || !toMatches
                        || !dateMatches)
                    {
                        continue;
                    }
                }

                vehicles.TryGetValue(
                    ride.DriverId,
                    out var vehicle);

                var genderBlocked =
                    ride.PreferredGender != "Any"
                    && !string.IsNullOrEmpty(
                        ride.PreferredGender)
                    && user.Gender !=
                       ride.PreferredGender;

                results.Add(
                    new RideResultViewModel
                    {
                        Id = ride.Id,

                        DriverName =
                            ride.Driver.FullName,

                        DriverRating =
                            ride.Driver.AverageRating,

                        DriverVerified =
                            ride.Driver.IsUcpVerified,

                        FromLocation =
                            ride.FromLocation,

                        ToLocation =
                            ride.ToLocation,

                        RideDate =
                            ride.RideDate,

                        DepartureTime =
                            ride.DepartureTime,

                        AvailableSeats =
                            ride.AvailableSeats,

                        Contribution =
                            ride.Contribution,

                        PreferredGender =
                            ride.PreferredGender,

                        VehicleModel =
                            vehicle != null
                                ? $"{vehicle.Model} ({vehicle.VehicleType})"
                                : "N/A",

                        MatchScore =
                            Math.Min(score, 100),

                        AlreadyRequested =
                            ride.RideRequests.Any(
                                rr =>
                                    rr.PassengerId ==
                                    user.Id),

                        GenderBlocked =
                            genderBlocked
                    });
            }

            model.Results =
                results
                    .OrderByDescending(
                        r => r.MatchScore)
                    .ToList();

            return View(model);
        }

        // =========================================================
        // REQUEST RIDE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRide(
            int id,
            string? message)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var ride =
                await _context.Rides
                    .Include(r => r.RideSeats)
                    .FirstOrDefaultAsync(
                        r => r.Id == id);

            if (ride == null)
                return NotFound();

            if (ride.DriverId == user.Id)
            {
                TempData["Error"] =
                    "You can't request your own ride.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // Gender restriction
            if (
                ride.PreferredGender != "Any"
                && !string.IsNullOrEmpty(
                    ride.PreferredGender)
                && user.Gender !=
                   ride.PreferredGender)
            {
                TempData["Error"] =
                    $"This ride is reserved for {ride.PreferredGender} passengers only.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (
                ride.Status != "Active"
                || ride.AvailableSeats <= 0
                || ride.RideDate.Date < DateTime.Today)
            {
                TempData["Error"] =
                    "This ride is no longer available.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var alreadyRequested =
                await _context.RideRequests.AnyAsync(
                    rr =>
                        rr.RideId == id
                        && rr.PassengerId == user.Id);

            if (alreadyRequested)
            {
                TempData["Info"] =
                    "You've already requested this ride.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var request = new RideRequest
            {
                RideId = id,
                PassengerId = user.Id,
                Status = "Pending",
                RequestDate = DateTime.Now,
                Message = message
            };

            _context.RideRequests.Add(request);

            await _context.SaveChangesAsync();

            // Notify driver
            _context.Notifications.Add(
                new Notification
                {
                    UserId = ride.DriverId,
                    Title = "New Ride Request",
                    Message =
                        $"{user.FullName} has requested to join your ride from {ride.FromLocation} to {ride.ToLocation}.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Request sent! The driver will review it soon.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // BOOK SEAT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookSeat(
            int rideId,
            int seatId)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var ride =
                await _context.Rides
                    .Include(r => r.RideSeats)
                    .FirstOrDefaultAsync(
                        r => r.Id == rideId);

            if (ride == null)
                return NotFound();

            // Driver cannot book own seat
            if (ride.DriverId == user.Id)
            {
                TempData["Error"] =
                    "You cannot book a seat on your own ride.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = rideId });
            }

            // Ride validation
            if (ride.Status != "Active"
                || ride.RideDate.Date < DateTime.Today)
            {
                TempData["Error"] =
                    "This ride is not available.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = rideId });
            }

            // Gender restriction
            if (ride.PreferredGender != "Any"
                && !string.IsNullOrEmpty(
                    ride.PreferredGender)
                && user.Gender != ride.PreferredGender)
            {
                TempData["Error"] =
                    $"This ride is reserved for {ride.PreferredGender} passengers only.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = rideId });
            }

            // Must have accepted request
            var request =
                await _context.RideRequests
                    .FirstOrDefaultAsync(
                        rr =>
                            rr.RideId == rideId
                            && rr.PassengerId == user.Id
                            && rr.Status == "Accepted");

            if (request == null)
            {
                TempData["Error"] =
                    "Your ride request must be accepted by the driver before booking a seat.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = rideId });
            }

            // Find seat
            var seat =
                await _context.RideSeats
                    .FirstOrDefaultAsync(
                        s =>
                            s.Id == seatId
                            && s.RideId == rideId);

            if (seat == null)
            {
                TempData["Error"] =
                    "Seat not found.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = rideId });
            }

            if (seat.Status != "Available")
            {
                TempData["Error"] =
                    "This seat is already booked.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = rideId });
            }

            // User cannot book multiple seats
            var alreadyBooked =
                ride.RideSeats.Any(
                    s =>
                        s.BookedByUserId == user.Id);

            if (alreadyBooked)
            {
                TempData["Info"] =
                    "You already have a seat booked for this ride.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = rideId });
            }

            // =====================================================
            // BOOK
            // =====================================================

            seat.Status = "Booked";
            seat.BookedByUserId = user.Id;

            if (ride.AvailableSeats > 0)
                ride.AvailableSeats--;

            if (ride.AvailableSeats == 0)
                ride.Status = "Full";

            await _context.SaveChangesAsync();

            // Notify driver
            _context.Notifications.Add(
                new Notification
                {
                    UserId = ride.DriverId,
                    Title = "Seat Booked",
                    Message =
                        $"{user.FullName} booked seat {seat.SeatNumber} for your {ride.FromLocation} to {ride.ToLocation} ride.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Seat {seat.SeatNumber} booked successfully!";

            return RedirectToAction(
                nameof(Details),
                new { id = rideId });
        }

        // =========================================================
        // CANCEL MY SEAT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSeat(
            int rideId)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var ride =
                await _context.Rides
                    .Include(r => r.RideSeats)
                    .FirstOrDefaultAsync(
                        r => r.Id == rideId);

            if (ride == null)
                return NotFound();

            var seat =
                ride.RideSeats.FirstOrDefault(
                    s =>
                        s.BookedByUserId == user.Id
                        && s.Status == "Booked");

            if (seat == null)
            {
                TempData["Error"] =
                    "You don't have a booked seat on this ride.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = rideId });
            }

            seat.Status = "Available";
            seat.BookedByUserId = null;

            ride.AvailableSeats++;

            if (ride.Status == "Full")
                ride.Status = "Active";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Seat {seat.SeatNumber} has been released.";

            return RedirectToAction(
                nameof(Details),
                new { id = rideId });
        }
    }
}