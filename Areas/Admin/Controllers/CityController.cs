using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TripAnalyzer.Data;
using TripAnalyzer.Models;

namespace TripAnalyzer.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CityController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CityController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? search, int? districtFilter)
        {
            var query = _db.Cities.Include(c => c.District).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var sLower = search.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(sLower) || c.CityCode.ToLower().Contains(sLower));
            }

            if (districtFilter.HasValue)
            {
                query = query.Where(c => c.DistrictId == districtFilter.Value);
            }

            var cities = await query.OrderBy(c => c.Name).ToListAsync();
            var districts = await _db.Districts.OrderBy(d => d.Name).ToListAsync();

            ViewBag.Districts = new SelectList(districts, "Id", "Name", districtFilter);
            ViewBag.Search = search;
            ViewBag.DistrictFilter = districtFilter;

            return View(cities);
        }

        public async Task<IActionResult> Create()
        {
            var districts = await _db.Districts.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.Districts = new SelectList(districts, "Id", "Name");
            return View(new City());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(City city)
        {
            if (ModelState.IsValid)
            {
                _db.Cities.Add(city);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "City created successfully.";
                return RedirectToAction(nameof(Index));
            }

            var districts = await _db.Districts.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.Districts = new SelectList(districts, "Id", "Name", city.DistrictId);
            return View(city);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var city = await _db.Cities.FindAsync(id);
            if (city == null) return NotFound();

            var districts = await _db.Districts.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.Districts = new SelectList(districts, "Id", "Name", city.DistrictId);
            return View(city);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(City city)
        {
            if (ModelState.IsValid)
            {
                _db.Cities.Update(city);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "City updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            var districts = await _db.Districts.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.Districts = new SelectList(districts, "Id", "Name", city.DistrictId);
            return View(city);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var city = await _db.Cities.FindAsync(id);
            if (city != null)
            {
                city.IsActive = !city.IsActive;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"City status updated.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
