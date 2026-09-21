using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TripAnalyzer.Data;
using TripAnalyzer.Models;

namespace TripAnalyzer.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class FareController : Controller
    {
        private readonly ApplicationDbContext _db;

        public FareController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var fares = await _db.FareConfigurations.OrderBy(f => f.Mode).ToListAsync();
            return View(fares);
        }

        public IActionResult Create()
        {
            return View(new FareConfiguration());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FareConfiguration fare)
        {
            if (ModelState.IsValid)
            {
                fare.UpdatedAt = System.DateTime.UtcNow;
                _db.FareConfigurations.Add(fare);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Fare configuration created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(fare);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var fare = await _db.FareConfigurations.FindAsync(id);
            if (fare == null) return NotFound();
            return View(fare);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FareConfiguration fare)
        {
            if (ModelState.IsValid)
            {
                fare.UpdatedAt = System.DateTime.UtcNow;
                _db.FareConfigurations.Update(fare);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Fare configuration updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(fare);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var fare = await _db.FareConfigurations.FindAsync(id);
            if (fare != null)
            {
                fare.IsActive = !fare.IsActive;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Fare configuration status updated.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
