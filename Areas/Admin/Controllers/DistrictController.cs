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
    public class DistrictController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DistrictController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? search, string? stateFilter)
        {
            var query = _db.Districts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var sLower = search.Trim().ToLower();
                query = query.Where(d => d.Name.ToLower().Contains(sLower) || d.DistrictCode.ToLower().Contains(sLower));
            }

            if (!string.IsNullOrWhiteSpace(stateFilter))
            {
                query = query.Where(d => d.State == stateFilter);
            }

            var districts = await query.OrderBy(d => d.Name).ToListAsync();
            
            var cityCounts = await _db.Cities
                .GroupBy(c => c.DistrictId)
                .Select(g => new { DistrictId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DistrictId, x => x.Count);

            var states = await _db.Districts.Select(d => d.State).Distinct().OrderBy(s => s).ToListAsync();

            ViewBag.CityCounts = cityCounts;
            ViewBag.States = states;
            ViewBag.Search = search;
            ViewBag.StateFilter = stateFilter;

            return View(districts);
        }

        public IActionResult Create()
        {
            return View(new District());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(District district)
        {
            if (ModelState.IsValid)
            {
                _db.Districts.Add(district);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "District created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(district);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var district = await _db.Districts.FindAsync(id);
            if (district == null) return NotFound();
            return View(district);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(District district)
        {
            if (ModelState.IsValid)
            {
                _db.Districts.Update(district);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "District updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(district);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var district = await _db.Districts.FindAsync(id);
            if (district != null)
            {
                district.IsActive = !district.IsActive;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"District status updated.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
