using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TripAnalyzer.Data;
using TripAnalyzer.Models;

namespace TripAnalyzer.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<ChatbotService> _logger;

        private static readonly List<string> StaticSuggestions = new List<string>
        {
            "How does Trip Analyzer work?",
            "Which transport is cheapest?",
            "How is the recommendation score calculated?",
            "Is the ticket data live or estimated?"
        };

        public ChatbotService(ApplicationDbContext db, IConfiguration config, ILogger<ChatbotService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
            
            var model = _config["GEMINI_MODEL"] ?? Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-3.6-flash";
            var apiKey = _config["GEMINI_API_KEY"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            _logger.LogInformation("Gemini model configured: {Model}", model);
            _logger.LogInformation("Gemini API key loaded: {IsKeyLoaded}", !string.IsNullOrWhiteSpace(apiKey));
        }

        public async Task<List<ChatbotFAQ>> GetActiveFaqsAsync()
        {
            return await _db.ChatbotFAQs
                .Where(f => f.IsActive)
                .OrderBy(f => f.DisplayOrder)
                .ThenBy(f => f.Question)
                .ToListAsync();
        }

        public async Task<ChatbotResponse> GetAnswerAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return new ChatbotResponse
                {
                    Answer = "Hello! I am your Trip Assistant. How can I help you optimize your travel today?",
                    Category = "Welcome",
                    SuggestedQuestions = StaticSuggestions
                };
            }

            // Try Gemini first
            var geminiAnswer = await GetGeminiAnswerAsync(userMessage);
            if (!string.IsNullOrWhiteSpace(geminiAnswer))
            {
                return new ChatbotResponse
                {
                    Answer = geminiAnswer,
                    Category = "AI",
                    SuggestedQuestions = StaticSuggestions,
                    IsAiGenerated = true
                };
            }

            // Fallback to local matching logic
            var queryLower = userMessage.Trim().ToLowerInvariant();
            var activeFaqsList = await GetActiveFaqsAsync();

            // Try exact question match
            var exactMatch = activeFaqsList.FirstOrDefault(f => f.Question.Equals(userMessage.Trim(), StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
            {
                return new ChatbotResponse
                {
                    Answer = exactMatch.Answer,
                    Category = exactMatch.Category,
                    SuggestedQuestions = StaticSuggestions
                };
            }

            // Keyword scoring match
            ChatbotFAQ? bestMatch = null;
            int highestScore = 0;

            var words = queryLower.Split(new[] { ' ', '?', '!', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var faq in activeFaqsList)
            {
                int score = 0;
                var faqQuestionLower = faq.Question.ToLowerInvariant();
                var keywords = (faq.Keywords ?? "").ToLowerInvariant().Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in words)
                {
                    if (word.Length < 3) continue; // Skip short stop-words

                    if (faqQuestionLower.Contains(word)) score += 3;
                    if (keywords.Any(k => k.Equals(word, StringComparison.OrdinalIgnoreCase))) score += 5;
                }

                if (score > highestScore)
                {
                    highestScore = score;
                    bestMatch = faq;
                }
            }

            if (bestMatch != null && highestScore >= 3)
            {
                return new ChatbotResponse
                {
                    Answer = bestMatch.Answer,
                    Category = bestMatch.Category,
                    SuggestedQuestions = StaticSuggestions
                };
            }

            // Fallback response if no close match is found
            return new ChatbotResponse
            {
                Answer = "I'm sorry, I couldn't find an exact match for your question. You can ask about recommendations, transport pricing, or contact support directly!",
                Category = "Help",
                SuggestedQuestions = StaticSuggestions
            };
        }

        private async Task<string?> GetGeminiAnswerAsync(string userMessage)
        {
            var apiKey = _config["GEMINI_API_KEY"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return null;
            }

            // Official Gemini REST API: generateContent endpoint (S-12 fix)
            var model = _config["GEMINI_MODEL"] ?? Environment.GetEnvironmentVariable("GEMINI_MODEL");
            if (string.IsNullOrWhiteSpace(model))
            {
                model = "gemini-3.6-flash";
            }
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
            
            _logger.LogInformation("Attempting Gemini API request. Model: {Model}, Endpoint: {Endpoint}, API Key Loaded: {KeyLoaded}", model, url, !string.IsNullOrWhiteSpace(apiKey));

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = "You are Trip Assistant for Trip Analyzer. Answer the user's actual question directly and naturally. Help with Trip Analyzer, travel planning, transport modes, estimated fares, estimated distances, recommendation scores and general questions. Never invent live availability, schedules, exact real-time fares, train names or flight details. Clearly label estimated or benchmark travel information. Be concise, helpful and professional." } }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = userMessage } }
                    }
                }
            };

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("Gemini API returned HTTP 404. Status Code: 404. Endpoint: {Endpoint}, Response: {ResponseJson}", url, responseJson);
                    }
                    else
                    {
                        _logger.LogWarning("Gemini API returned HTTP {StatusCode}. Falling back to FAQ matching.",
                            (int)response.StatusCode);
                    }
                    return null;
                }

                using var doc = JsonDocument.Parse(responseJson);

                // Standard generateContent response: candidates[0].content.parts[0].text
                if (doc.RootElement.TryGetProperty("candidates", out var candidates)
                    && candidates.GetArrayLength() > 0)
                {
                    var candidate = candidates[0];
                    if (candidate.TryGetProperty("content", out var contentEl)
                        && contentEl.TryGetProperty("parts", out var parts)
                        && parts.GetArrayLength() > 0)
                    {
                        var sb = new StringBuilder();
                        foreach (var part in parts.EnumerateArray())
                        {
                            if (part.TryGetProperty("text", out var textEl))
                            {
                                var txt = textEl.GetString();
                                if (!string.IsNullOrWhiteSpace(txt))
                                    sb.Append(txt);
                            }
                        }
                        var result = sb.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(result))
                            return result;
                    }
                }

                _logger.LogWarning("Gemini API returned 200 OK but no readable candidates. Falling back to FAQ matching.");
            }
            catch (Exception ex)
            {
                // Log the failure without exposing the API key or stack trace to the client (S-13 fix)
                _logger.LogWarning(ex, "Gemini API call failed. Falling back to FAQ database matching.");
            }

            return null;
        }
    }
}
