using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TripAnalyzer.Services;

namespace TripAnalyzer.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    public class ChatbotApiController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotApiController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        public class ChatRequestModel
        {
            public string? Message { get; set; }
        }

        // Anonymous but rate-limited (S-05): homepage chatbot requires unauthenticated access.
        // Strict per-IP rate limiting (20 req/min) is enforced via the "chatbot" policy.
        [HttpPost("ask")]
        [EnableRateLimiting("chatbot")]
        public async Task<IActionResult> Ask([FromBody] ChatRequestModel request)
        {
            // Request-size guard: reject messages exceeding 500 characters
            var message = request?.Message ?? string.Empty;
            if (message.Length > 500)
            {
                return BadRequest(new { error = "Message exceeds the maximum allowed length of 500 characters." });
            }

            var response = await _chatbotService.GetAnswerAsync(message);
            return Ok(response);
        }

        [HttpGet("faqs")]
        [EnableRateLimiting("chatbot")]
        public async Task<IActionResult> GetFaqs()
        {
            var faqs = await _chatbotService.GetActiveFaqsAsync();
            return Ok(faqs);
        }
    }
}
