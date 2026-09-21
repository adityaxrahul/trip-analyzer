using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TripAnalyzer.Data;
using TripAnalyzer.Services;
using TripAnalyzer.ViewModels;

namespace TripAnalyzer.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly IAdminAnalyticsService _analyticsService;
        private readonly ApplicationDbContext _db;

        public DashboardController(IAdminAnalyticsService analyticsService, ApplicationDbContext db)
        {
            _analyticsService = analyticsService;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var analytics = await _analyticsService.GetAnalyticsDataAsync();

            var recentTrips = await _db.Trips
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentEnquiries = await _db.Enquiries
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentUsers = await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            var viewModel = new AdminDashboardViewModel
            {
                Analytics = analytics,
                RecentTrips = recentTrips,
                RecentEnquiries = recentEnquiries,
                RecentUsers = recentUsers
            };

            return View(viewModel);
        }
    }
}
