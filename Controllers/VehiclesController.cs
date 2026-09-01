using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UcpCarPool.Data;
using UcpCarPool.Models;

namespace UcpCarPool.Controllers
{
    [Authorize] // any logged-in Member can register a vehicle — no separate Driver role anymore
    public class VehiclesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VehiclesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var vehicles = await _context.Vehicles
                .Where(v => v.DriverId == user.Id)
                .ToListAsync();

            return View(vehicles);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == id && v.DriverId == user.Id);

            if (vehicle == null) return NotFound();

            return View(vehicle);
        }

        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var alreadyHasVehicle = await _context.Vehicles.AnyAsync(v => v.DriverId == user.Id);
            if (alreadyHasVehicle)
            {
                TempData["Info"] = "You already have a registered vehicle. Edit it below instead.";
                return RedirectToAction(nameof(Index));
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("VehicleType,Model,Color,PlateNumber,TotalSeats")] Vehicle vehicle)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var alreadyHasVehicle = await _context.Vehicles.AnyAsync(v => v.DriverId == user.Id);
            if (alreadyHasVehicle)
            {
                ModelState.AddModelError("", "You already have a registered vehicle.");
                return View(vehicle);
            }

            var plateTaken = await _context.Vehicles.AnyAsync(v => v.PlateNumber == vehicle.PlateNumber);
            if (plateTaken)
            {
                ModelState.AddModelError(nameof(Vehicle.PlateNumber), "This plate number is already registered.");
                return View(vehicle);
            }

            vehicle.DriverId = user.Id;
            vehicle.CreatedAt = DateTime.Now;

            ModelState.Remove(nameof(Vehicle.DriverId));
            ModelState.Remove(nameof(Vehicle.Driver));

            if (ModelState.IsValid)
            {
                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Vehicle registered successfully. You can now post rides!";
                return RedirectToAction(nameof(Index));
            }

            return View(vehicle);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == id && v.DriverId == user.Id);

            if (vehicle == null) return NotFound();

            return View(vehicle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,VehicleType,Model,Color,PlateNumber,TotalSeats")] Vehicle vehicle)
        {
            if (id != vehicle.Id) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var existingVehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == id && v.DriverId == user.Id);

            if (existingVehicle == null) return NotFound();

            var plateTaken = await _context.Vehicles
                .AnyAsync(v => v.PlateNumber == vehicle.PlateNumber && v.Id != id);
            if (plateTaken)
            {
                ModelState.AddModelError(nameof(Vehicle.PlateNumber), "This plate number is already registered.");
                return View(vehicle);
            }

            ModelState.Remove(nameof(Vehicle.DriverId));
            ModelState.Remove(nameof(Vehicle.Driver));
            ModelState.Remove(nameof(Vehicle.CreatedAt));

            if (ModelState.IsValid)
            {
                existingVehicle.VehicleType = vehicle.VehicleType;
                existingVehicle.Model = vehicle.Model;
                existingVehicle.Color = vehicle.Color;
                existingVehicle.PlateNumber = vehicle.PlateNumber;
                existingVehicle.TotalSeats = vehicle.TotalSeats;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Vehicle updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(vehicle);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == id && v.DriverId == user.Id);

            if (vehicle == null) return NotFound();

            return View(vehicle);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == id && v.DriverId == user.Id);

            if (vehicle == null) return NotFound();

            var hasActiveRides = await _context.Rides
                .AnyAsync(r => r.DriverId == user.Id && r.Status == "Active");

            if (hasActiveRides)
            {
                TempData["Error"] = "You can't delete your vehicle while you have active rides posted.";
                return RedirectToAction(nameof(Index));
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Vehicle deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
