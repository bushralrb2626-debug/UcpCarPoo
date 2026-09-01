using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UcpCarPool.Data;
using UcpCarPool.Models;
using UcpCarPool.ViewModels;
using System.Diagnostics;

namespace UcpCarPool.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? batch)
        {
            var model = new HomeFeedViewModel
            {
                SelectedBatch = batch
            };

            model.AvailableBatches = await _context.Users
                .Where(u => u.Batch != null)
                .Select(u => u.Batch!)
                .Distinct()
                .OrderByDescending(b => b)
                .ToListAsync();

            model.TotalActiveRides = await _context.Rides.CountAsync(r => r.Status == "Active" || r.Status == "Full");
            model.TotalMembers = await _context.Users.CountAsync();
            model.TotalCompletedRides = await _context.Rides.CountAsync(r => r.Status == "Completed");

            // Recently posted rides
            var rideQuery = _context.Rides
                .Include(r => r.Driver)
                .Where(r => r.Status == "Active" || r.Status == "Full" || r.Status == "Completed")
                .OrderByDescending(r => r.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(batch))
                rideQuery = rideQuery.Where(r => r.Driver!.Batch == batch);

            var recentRides = await rideQuery.Take(15).ToListAsync();

            var feed = new List<FeedItemViewModel>();

            foreach (var ride in recentRides)
            {
                if (ride.Driver == null) continue;

                if (ride.Status == "Completed")
                {
                    feed.Add(new FeedItemViewModel
                    {
                        ActorName = ride.Driver.FullName,
                        Batch = ride.Driver.Batch,
                        Verified = ride.Driver.IsUcpVerified,
                        Type = "RideCompleted",
                        Headline = $"{ride.Driver.FullName} completed a ride",
                        SubText = $"{ride.FromLocation} → {ride.ToLocation}",
                        When = ride.CreatedAt,
                        RideId = ride.Id
                    });
                }
                else
                {
                    feed.Add(new FeedItemViewModel
                    {
                        ActorName = ride.Driver.FullName,
                        Batch = ride.Driver.Batch,
                        Verified = ride.Driver.IsUcpVerified,
                        Type = "RidePosted",
                        Headline = $"{ride.Driver.FullName} posted a new ride",
                        SubText = $"{ride.FromLocation} → {ride.ToLocation} · {ride.RideDate:d MMM} · Rs.{ride.Contribution}/seat",
                        When = ride.CreatedAt,
                        RideId = ride.Id
                    });
                }
            }

            // Recently joined members (adds social/community feel)
            var memberQuery = _context.Users
                .Where(u => u.EmailConfirmed)
                .OrderByDescending(u => u.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(batch))
                memberQuery = memberQuery.Where(u => u.Batch == batch);

            var recentMembers = await memberQuery.Take(8).ToListAsync();

            foreach (var member in recentMembers)
            {
                feed.Add(new FeedItemViewModel
                {
                    ActorName = member.FullName,
                    Batch = member.Batch,
                    Verified = member.IsUcpVerified,
                    Type = "NewMember",
                    Headline = $"{member.FullName} joined UcpCarPool",
                    SubText = member.Batch != null ? $"Batch: {member.Batch}" : null,
                    When = member.CreatedAt
                });
            }

            model.FeedItems = feed.OrderByDescending(f => f.When).Take(20).ToList();

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
