using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TripAnalyzer.Data;
using TripAnalyzer.Services;
using System.Linq;

namespace TripAnalyzer.Controllers
{
    [Route("api/districts")]
    [ApiController]
    public class DistrictApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public DistrictApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("search")]
        [EnableRateLimiting("districts")]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new object[] { });
            }

            var qLower = q.Trim().ToLowerInvariant();

            // Search cities
            var cities = await _db.Cities
                .Include(c => c.District)
                .Where(c => c.IsActive && (
                    c.Name.ToLower().Contains(qLower) || 
                    c.State.ToLower().Contains(qLower) || 
                    c.StateCode.ToLower().Contains(qLower) ||
                    c.CityCode.ToLower().Contains(qLower)
                ))
                .Take(8)
                .ToListAsync();

            // Search districts
            var districts = await _db.Districts
                .Where(d => d.IsActive && (
                    d.Name.ToLower().Contains(qLower) || 
                    d.State.ToLower().Contains(qLower) || 
                    d.StateCode.ToLower().Contains(qLower) ||
                    d.DistrictCode.ToLower().Contains(qLower)
                ))
                .Take(8)
                .ToListAsync();

            var results = new List<object>();

            // Add cities first (more specific)
            foreach (var c in cities)
            {
                results.Add(new
                {
                    id = $"city-{c.Id}",
                    name = c.Name,
                    displayName = $"{c.Name}, {c.District?.Name ?? ""} District, {c.State}, India"
                });
            }

            // Add districts, avoiding exact duplicates if already represented as a city or if names match closely
            foreach (var d in districts)
            {
                if (cities.Any(c => c.Name.Equals(d.Name, StringComparison.OrdinalIgnoreCase) && c.State.Equals(d.State, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                results.Add(new
                {
                    id = $"dist-{d.Id}",
                    name = d.Name,
                    displayName = $"{d.Name} District, {d.State}, India"
                });
            }

            return Ok(results);
        }

        [HttpGet("nearest")]
        [EnableRateLimiting("districts")]
        public async Task<IActionResult> Nearest(double lat, double lng)
        {
            if (lat == 0 && lng == 0)
            {
                return BadRequest(new { error = "Invalid coordinates." });
            }

            var cities = await _db.Cities
                .Where(c => c.IsActive)
                .Select(c => new { c.Name, c.Latitude, c.Longitude })
                .ToListAsync();

            if (!cities.Any())
            {
                var districts = await _db.Districts
                    .Where(d => d.IsActive)
                    .Select(d => new { d.Name, d.Latitude, d.Longitude })
                    .ToListAsync();

                var closestDist = districts
                    .OrderBy(d => (d.Latitude - lat) * (d.Latitude - lat) + (d.Longitude - lng) * (d.Longitude - lng))
                    .FirstOrDefault();

                if (closestDist != null)
                {
                    return Ok(new { name = closestDist.Name });
                }

                return NotFound();
            }

            var closest = cities
                .OrderBy(c => (c.Latitude - lat) * (c.Latitude - lat) + (c.Longitude - lng) * (c.Longitude - lng))
                .FirstOrDefault();

            return Ok(new { name = closest?.Name ?? "" });
        }
    }
}
