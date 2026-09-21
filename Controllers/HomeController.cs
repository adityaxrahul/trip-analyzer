using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TripAnalyzer.Data;
using TripAnalyzer.Models;
using TripAnalyzer.Services;
using TripAnalyzer.ViewModels;

namespace TripAnalyzer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSenderService _emailSender;
        private readonly IChatbotService _chatbotService;

        public HomeController(ApplicationDbContext db, IEmailSenderService emailSender, IChatbotService chatbotService)
        {
            _db = db;
            _emailSender = emailSender;
            _chatbotService = chatbotService;
        }

        public async Task<IActionResult> Index()
        {
            var model = new TripSearchViewModel();
            
            // Statistics for hero section (B-03 fix: use real count only)
            ViewBag.TotalTripsCount = await _db.Trips.CountAsync();
            ViewBag.TotalDestinationsCount = await _db.TransportOptions.Select(t => t.Destination).Distinct().CountAsync();
            ViewBag.SatisfactionRate = "98.4%";

            return View(model);
        }

        public IActionResult HowItWorks()
        {
            return View();
        }

        public async Task<IActionResult> Faq()
        {
            var faqs = await _chatbotService.GetActiveFaqsAsync();
            return View(faqs);
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View(new ContactViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("contact")]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var enquiry = new Enquiry
            {
                UserId = userId,
                Name = model.Name.Trim(),
                Email = model.Email.Trim(),
                Subject = model.Subject.Trim(),
                Message = model.Message.Trim(),
                Status = "Pending",
                CreatedAt = System.DateTime.UtcNow
            };

            _db.Enquiries.Add(enquiry);
            await _db.SaveChangesAsync();

            bool emailSent = false;
            string? emailErrorMessage = null;

            try
            {
                // Dispatch admin email notification
                await _emailSender.SendAdminNotificationAsync(enquiry.Name, enquiry.Email, enquiry.Subject, enquiry.Message);
                emailSent = true;
            }
            catch (Exception ex)
            {
                emailErrorMessage = ex.Message;
            }

            model.IsSubmitted = true;
            ModelState.Clear();

            if (emailSent)
            {
                ViewBag.SuccessMessage = "Thank you for contacting Trip Analyzer! Your enquiry has been received and our team has been notified via email.";
            }
            else
            {
                ViewBag.ErrorMessage = $"Your enquiry was saved successfully, but the email notification to our administrators failed: {emailErrorMessage}";
            }

            return View(new ContactViewModel { IsSubmitted = true });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
