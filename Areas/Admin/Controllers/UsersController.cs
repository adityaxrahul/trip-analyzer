using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TripAnalyzer.Data;
using TripAnalyzer.Models;
using TripAnalyzer.ViewModels;

namespace TripAnalyzer.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(term) ||
                                         (u.Email != null && u.Email.ToLower().Contains(term)) ||
                                         (u.MobileNumber != null && u.MobileNumber.Contains(term)));
            }

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            ViewBag.SearchTerm = search;

            return View(users);
        }

        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            int tripCount = await _db.Trips.CountAsync(t => t.UserId == id);
            int savedCount = await _db.SavedTrips.CountAsync(s => s.UserId == id);
            int enquiryCount = await _db.Enquiries.CountAsync(e => e.UserId == id);

            var userTrips = await _db.Trips
                .Where(t => t.UserId == id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.UserTrips = userTrips;

            var model = new UserManageViewModel
            {
                User = user,
                Roles = roles,
                TripCount = tripCount,
                SavedCount = savedCount,
                EnquiryCount = enquiryCount
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = $"User status updated to {(user.IsActive ? "Active" : "Deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdminRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            bool isSelf = user.Id == currentUserId;

            bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                if (isSelf)
                {
                    TempData["ErrorMessage"] = "You cannot revoke your own administrator role.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _userManager.RemoveFromRoleAsync(user, "Admin");
                TempData["SuccessMessage"] = $"Administrator role revoked for {user.FullName}.";
            }
            else
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                TempData["SuccessMessage"] = $"Administrator role granted to {user.FullName}.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
