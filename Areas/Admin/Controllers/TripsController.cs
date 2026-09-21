using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TripAnalyzer.Data;

namespace TripAnalyzer.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TripsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public TripsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _db.Trips
                .Include(t => t.User)
                .Include(t => t.Analyses)
                    .ThenInclude(a => a.TransportOption)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                query = query.Where(t => t.Source.ToLower().Contains(term) ||
                                         t.Destination.ToLower().Contains(term) ||
                                         (t.User != null && t.User.FullName.ToLower().Contains(term)));
            }

            var trips = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            ViewBag.SearchTerm = search;

            return View(trips);
        }

        public async Task<IActionResult> Details(int id)
        {
            var trip = await _db.Trips
                .Include(t => t.User)
                .Include(t => t.Analyses)
                    .ThenInclude(a => a.TransportOption)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trip == null) return NotFound();

            return View(trip);
        }
    }
}
