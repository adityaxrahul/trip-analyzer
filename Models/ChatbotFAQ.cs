using System;

namespace TripAnalyzer.Models
{
    public class ChatbotFAQ
    {
        public int Id { get; set; }

        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Category { get; set; } = "General"; // General, Recommendation, Pricing, Account, Support
        public string Keywords { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
