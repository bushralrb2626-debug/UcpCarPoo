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
    public class RatingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RatingsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Ratings/Rate?rideId=5&toUserId=xyz
        public async Task<IActionResult> Rate(int rideId, string toUserId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var (allowed, error, ride, toUser) = await ValidateRatingEligibility(user.Id, rideId, toUserId);
            if (!allowed)
            {
                TempData["Error"] = error;
                return RedirectToAction("Details", "Rides", new { id = rideId });
            }

            var model = new RateViewModel
            {
                RideId = rideId,
                ToUserId = toUserId,
                ToUserName = toUser!.FullName,
                RouteLabel = $"{ride!.FromLocation} → {ride.ToLocation} ({ride.RideDate:d MMM yyyy})"
            };

            return View(model);
        }

        // POST: Ratings/Rate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(RateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var (allowed, error, ride, toUser) = await ValidateRatingEligibility(user.Id, model.RideId, model.ToUserId);
            if (!allowed)
            {
                TempData["Error"] = error;
                return RedirectToAction("Details", "Rides", new { id = model.RideId });
            }

            if (!ModelState.IsValid)
            {
                model.ToUserName = toUser!.FullName;
                model.RouteLabel = $"{ride!.FromLocation} → {ride.ToLocation} ({ride.RideDate:d MMM yyyy})";
                return View(model);
            }

            var rating = new Rating
            {
                RideId = model.RideId,
                FromUserId = user.Id,
                ToUserId = model.ToUserId,
                Stars = model.Stars,
                Comment = model.Comment,
                CreatedAt = DateTime.Now
            };

            _context.Ratings.Add(rating);

            // Recalculate the target user's rolling average
            toUser!.TotalRatings += 1;
            toUser.AverageRating =
                ((toUser.AverageRating * (toUser.TotalRatings - 1)) + model.Stars) / toUser.TotalRatings;

            _context.Notifications.Add(new Notification
            {
                UserId = toUser.Id,
                Title = "New Rating Received",
                Message = $"{user.FullName} rated you {model.Stars} star(s) for your ride together.",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Thanks for your feedback!";
            return RedirectToAction("Details", "Rides", new { id = model.RideId });
        }

        // Shared eligibility check for both GET and POST, so the rules can never
        // be bypassed by posting directly to the form.
        private async Task<(bool allowed, string error, Ride? ride, ApplicationUser? toUser)>
            ValidateRatingEligibility(string fromUserId, int rideId, string toUserId)
        {
            var ride = await _context.Rides
                .Include(r => r.RideRequests)
                .FirstOrDefaultAsync(r => r.Id == rideId);

            if (ride == null)
                return (false, "Ride not found.", null, null);

            if (ride.Status != "Completed")
                return (false, "You can only rate after the ride is marked completed.", null, null);

            var isDriver = ride.DriverId == fromUserId;
            var isAcceptedPassenger = ride.RideRequests.Any(rr =>
                rr.PassengerId == fromUserId && rr.Status == "Accepted");

            if (!isDriver && !isAcceptedPassenger)
                return (false, "You weren't part of this ride.", null, null);

            // Driver can only rate an accepted passenger; passenger can only rate the driver
            var toUserIsValidTarget = isDriver
                ? ride.RideRequests.Any(rr => rr.PassengerId == toUserId && rr.Status == "Accepted")
                : ride.DriverId == toUserId;

            if (!toUserIsValidTarget)
                return (false, "Invalid rating target for this ride.", null, null);

            var alreadyRated = await _context.Ratings
                .AnyAsync(r => r.RideId == rideId && r.FromUserId == fromUserId && r.ToUserId == toUserId);

            if (alreadyRated)
                return (false, "You've already rated this person for this ride.", null, null);

            var toUser = await _userManager.FindByIdAsync(toUserId);
            if (toUser == null)
                return (false, "User not found.", null, null);

            return (true, string.Empty, ride, toUser);
        }
    }
}
