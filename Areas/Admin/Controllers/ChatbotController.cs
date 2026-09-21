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
    public class ChatbotController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ChatbotController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var faqs = await _db.ChatbotFAQs
                .OrderBy(f => f.DisplayOrder)
                .ThenBy(f => f.Question)
                .ToListAsync();

            return View(faqs);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ChatbotFaqManageViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChatbotFaqManageViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var faq = new ChatbotFAQ
            {
                Question = model.Question.Trim(),
                Answer = model.Answer.Trim(),
                Category = model.Category.Trim(),
                Keywords = model.Keywords.Trim(),
                DisplayOrder = model.DisplayOrder,
                IsActive = model.IsActive
            };

            _db.ChatbotFAQs.Add(faq);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "New Chatbot FAQ added successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var faq = await _db.ChatbotFAQs.FindAsync(id);
            if (faq == null) return NotFound();

            var model = new ChatbotFaqManageViewModel
            {
                Id = faq.Id,
                Question = faq.Question,
                Answer = faq.Answer,
                Category = faq.Category,
                Keywords = faq.Keywords,
                DisplayOrder = faq.DisplayOrder,
                IsActive = faq.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ChatbotFaqManageViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var faq = await _db.ChatbotFAQs.FindAsync(model.Id);
            if (faq == null) return NotFound();

            faq.Question = model.Question.Trim();
            faq.Answer = model.Answer.Trim();
            faq.Category = model.Category.Trim();
            faq.Keywords = model.Keywords.Trim();
            faq.DisplayOrder = model.DisplayOrder;
            faq.IsActive = model.IsActive;

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Chatbot FAQ updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var faq = await _db.ChatbotFAQs.FindAsync(id);
            if (faq != null)
            {
                _db.ChatbotFAQs.Remove(faq);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "FAQ deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var faq = await _db.ChatbotFAQs.FindAsync(id);
            if (faq != null)
            {
                faq.IsActive = !faq.IsActive;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"FAQ status toggled to {(faq.IsActive ? "Active" : "Inactive")}.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
