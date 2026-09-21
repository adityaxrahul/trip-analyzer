using System.Collections.Generic;
using System.Threading.Tasks;
using TripAnalyzer.Models;

namespace TripAnalyzer.Services
{
    public class ChatbotResponse
    {
        public string Answer { get; set; } = string.Empty;
        public string? Category { get; set; }
        public List<string> SuggestedQuestions { get; set; } = new List<string>();
        public bool IsAiGenerated { get; set; } = false;
    }

    public interface IChatbotService
    {
        Task<ChatbotResponse> GetAnswerAsync(string userMessage);
        Task<List<ChatbotFAQ>> GetActiveFaqsAsync();
    }
}
