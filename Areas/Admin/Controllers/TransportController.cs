using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TripAnalyzer.Data;
using TripAnalyzer.Models;
using TripAnalyzer.ViewModels;

namespace TripAnalyzer.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TransportController : Controller
    {
        private readonly ApplicationDbContext _db;

        public TransportController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? mode, string? search)
        {
            var query = _db.TransportOptions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(mode))
            {
                query = query.Where(t => t.Mode == mode);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                query = query.Where(t => t.Source.ToLower().Contains(term) ||
                                         t.Destination.ToLower().Contains(term) ||
                                         t.CarrierName.ToLower().Contains(term));
            }

            var options = await query
                .OrderBy(t => t.Source)
                .ThenBy(t => t.Destination)
                .ThenBy(t => t.Mode)
                .ToListAsync();

            ViewBag.SelectedMode = mode;
            ViewBag.SearchTerm = search;

            return View(options);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new TransportOptionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransportOptionViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var option = new TransportOption
            {
                Source = model.Source.Trim(),
                Destination = model.Destination.Trim(),
                Mode = model.Mode,
                CarrierName = model.CarrierName.Trim(),
                BasePricePerPassenger = model.BasePricePerPassenger,
                DurationMinutes = model.DurationMinutes,
                DistanceKm = model.DistanceKm,
                AvailabilityScore = model.AvailabilityScore,
                ComfortScore = model.ComfortScore,
                EcoRating = model.EcoRating,
                DepartureTime = model.DepartureTime.Trim(),
                ArrivalTime = model.ArrivalTime.Trim(),
                Amenities = model.Amenities.Trim(),
                IsActive = model.IsActive,
                IsEstimatedData = model.IsEstimatedData
            };

            _db.TransportOptions.Add(option);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "New transport option added successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var option = await _db.TransportOptions.FindAsync(id);
            if (option == null) return NotFound();

            var model = new TransportOptionViewModel
            {
                Id = option.Id,
                Source = option.Source,
                Destination = option.Destination,
                Mode = option.Mode,
                CarrierName = option.CarrierName,
                BasePricePerPassenger = option.BasePricePerPassenger,
                DurationMinutes = option.DurationMinutes,
                DistanceKm = option.DistanceKm,
                AvailabilityScore = option.AvailabilityScore,
                ComfortScore = option.ComfortScore,
                EcoRating = option.EcoRating,
                DepartureTime = option.DepartureTime,
                ArrivalTime = option.ArrivalTime,
                Amenities = option.Amenities,
                IsActive = option.IsActive,
                IsEstimatedData = option.IsEstimatedData
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TransportOptionViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var option = await _db.TransportOptions.FindAsync(model.Id);
            if (option == null) return NotFound();

            option.Source = model.Source.Trim();
            option.Destination = model.Destination.Trim();
            option.Mode = model.Mode;
            option.CarrierName = model.CarrierName.Trim();
            option.BasePricePerPassenger = model.BasePricePerPassenger;
            option.DurationMinutes = model.DurationMinutes;
            option.DistanceKm = model.DistanceKm;
            option.AvailabilityScore = model.AvailabilityScore;
            option.ComfortScore = model.ComfortScore;
            option.EcoRating = model.EcoRating;
            option.DepartureTime = model.DepartureTime.Trim();
            option.ArrivalTime = model.ArrivalTime.Trim();
            option.Amenities = model.Amenities.Trim();
            option.IsActive = model.IsActive;
            option.IsEstimatedData = model.IsEstimatedData;

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Transport option updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var option = await _db.TransportOptions.FindAsync(id);
            if (option != null)
            {
                _db.TransportOptions.Remove(option);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Transport option deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var option = await _db.TransportOptions.FindAsync(id);
            if (option != null)
            {
                option.IsActive = !option.IsActive;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Transport option status updated to {(option.IsActive ? "Active" : "Inactive")}.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
