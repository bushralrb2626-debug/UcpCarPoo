using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UcpCarPool.Data;

namespace UcpCarPool.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Admin Dashboard";

            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalRides = await _context.Rides.CountAsync();
            ViewBag.ActiveRides = await _context.Rides.CountAsync(r => r.Status == "Active" || r.Status == "Full");
            ViewBag.TotalVehicles = await _context.Vehicles.CountAsync();

            return View();
        }
    }
}
