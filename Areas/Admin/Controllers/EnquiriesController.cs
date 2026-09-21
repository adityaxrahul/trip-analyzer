using System;
using System.Linq;
using System.Net;
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
    public class EnquiriesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSenderService _emailSender;

        public EnquiriesController(ApplicationDbContext db, IEmailSenderService emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index(string? status, string? search)
        {
            var query = _db.Enquiries
                .Include(e => e.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(e => e.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                query = query.Where(e => e.Name.ToLower().Contains(term) ||
                                         e.Email.ToLower().Contains(term) ||
                                         e.Subject.ToLower().Contains(term) ||
                                         e.Message.ToLower().Contains(term));
            }

            var list = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
            ViewBag.SelectedStatus = status;
            ViewBag.SearchTerm = search;

            return View(list);
        }

        public async Task<IActionResult> Details(int id)
        {
            var enquiry = await _db.Enquiries
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enquiry == null) return NotFound();

            return View(enquiry);
        }

        [HttpGet]
        public async Task<IActionResult> Reply(int id)
        {
            var enquiry = await _db.Enquiries.FindAsync(id);
            if (enquiry == null) return NotFound();

            var model = new EnquiryManageViewModel
            {
                Enquiry = enquiry,
                AdminReply = enquiry.AdminReply ?? string.Empty,
                Status = enquiry.Status == "Pending" ? "Resolved" : enquiry.Status
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(EnquiryManageViewModel model)
        {
            var enquiry = await _db.Enquiries.FindAsync(model.Enquiry.Id);
            if (enquiry == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.AdminReply))
            {
                ModelState.AddModelError("AdminReply", "Response message cannot be blank.");
                model.Enquiry = enquiry;
                return View(model);
            }

            enquiry.AdminReply = model.AdminReply.Trim();

            // Enforce status allowlist — prevent arbitrary status values (S-16 / B-10)
            var allowedStatuses = new[] { "Pending", "In Progress", "Resolved" };
            if (!allowedStatuses.Contains(model.Status))
            {
                ModelState.AddModelError("Status", "Invalid status value. Allowed values are: Pending, In Progress, Resolved.");
                model.Enquiry = enquiry;
                return View(model);
            }
            enquiry.Status = model.Status;
            enquiry.RepliedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Dispatch reply email to user — all user-controlled values HTML-encoded (S-08)
            string safeName = WebUtility.HtmlEncode(enquiry.Name ?? "");
            string safeReply = WebUtility.HtmlEncode(enquiry.AdminReply)
                                         .Replace("\r\n", "<br/>")
                                         .Replace("\n", "<br/>")
                                         .Replace("\r", "<br/>");
            string safeSubjectText = (enquiry.Subject ?? "Your Enquiry").Replace("\r", "").Replace("\n", "");
            string subject = $"Re: {safeSubjectText}";
            string body = $@"
                <p>Dear {safeName},</p>
                <p>Thank you for contacting <strong>Trip Analyzer</strong>. Here is the response to your inquiry:</p>
                <blockquote style='background: #f3f4f6; padding: 12px; border-left: 4px solid #3b82f6;'>
                    {safeReply}
                </blockquote>
                <p>If you have any further questions, feel free to reply directly to this email.</p>
                <p>Best regards,<br/><strong>Trip Analyzer Support Team</strong></p>";

            bool emailSent = false;
            string? emailErrorMessage = null;

            try
            {
                await _emailSender.SendEmailAsync(enquiry.Email, subject, body);
                emailSent = true;
            }
            catch (Exception ex)
            {
                emailErrorMessage = ex.Message;
            }

            if (emailSent)
            {
                TempData["SuccessMessage"] = "Reply sent successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Reply could not be sent";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var enquiry = await _db.Enquiries.FindAsync(id);
            if (enquiry != null)
            {
                var allowed = new[] { "Pending", "In Progress", "Resolved" };
                if (!allowed.Contains(status))
                {
                    TempData["ErrorMessage"] = "Invalid status value.";
                    return RedirectToAction(nameof(Index));
                }
                enquiry.Status = status;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Enquiry status updated to '{status}'.";
            }

            // Redirect back to referring page only if it is a local URL (S-14: open redirect fix)
            string referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            {
                return Redirect(referer);
            }
            return RedirectToAction(nameof(Index));
        }

        // Shortcut to resolve an enquiry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            var enquiry = await _db.Enquiries.FindAsync(id);
            if (enquiry != null)
            {
                enquiry.Status = "Resolved";
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Enquiry resolved successfully.";
            }

            // Redirect back to referring page only if it is a local URL (S-14: open redirect fix)
            string referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            {
                return Redirect(referer);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var enquiry = await _db.Enquiries.FindAsync(id);
            if (enquiry != null)
            {
                _db.Enquiries.Remove(enquiry);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Enquiry deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
