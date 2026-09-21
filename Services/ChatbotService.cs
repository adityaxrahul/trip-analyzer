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

        private static readonly List<string> StaticSuggestions = new()
        {
            "How does Trip Analyzer work?",
            "Which transport is cheapest?",
            "How is the recommendation score calculated?",
            "Is the ticket data live or estimated?"
        };

        public ChatbotService(
            ApplicationDbContext db,
            IConfiguration config,
            ILogger<ChatbotService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;

            var model =
                _config["GEMINI_MODEL"]
                ?? Environment.GetEnvironmentVariable("GEMINI_MODEL")
                ?? "gemini-3.6-flash";

            var apiKey =
                _config["GEMINI_API_KEY"]
                ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

            _logger.LogInformation(
                "Gemini model configured: {Model}",
                model);

            _logger.LogInformation(
                "Gemini API key loaded: {IsKeyLoaded}",
                !string.IsNullOrWhiteSpace(apiKey));
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
                    Answer =
                        "Hello! I am your Trip Assistant. How can I help you plan your trip today?",
                    Category = "Welcome",
                    SuggestedQuestions = StaticSuggestions,
                    IsAiGenerated = false
                };
            }

            userMessage = userMessage.Trim();

            // =========================================================
            // 1. TRY GEMINI FIRST
            // =========================================================

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

            // =========================================================
            // 2. FAQ FALLBACK
            // =========================================================

            var activeFaqs = await GetActiveFaqsAsync();

            // Exact match
            var exactMatch = activeFaqs.FirstOrDefault(f =>
                string.Equals(
                    f.Question,
                    userMessage,
                    StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                return new ChatbotResponse
                {
                    Answer = exactMatch.Answer,
                    Category = exactMatch.Category,
                    SuggestedQuestions = StaticSuggestions,
                    IsAiGenerated = false
                };
            }

            // Keyword matching
            var queryWords = userMessage
                .ToLowerInvariant()
                .Split(
                    new[] { ' ', '?', '!', ',', '.', ':', ';', '-' },
                    StringSplitOptions.RemoveEmptyEntries);

            ChatbotFAQ? bestMatch = null;
            var highestScore = 0;

            foreach (var faq in activeFaqs)
            {
                var score = 0;

                var question = (faq.Question ?? "").ToLowerInvariant();

                var keywords = (faq.Keywords ?? "")
                    .ToLowerInvariant()
                    .Split(
                        new[] { ',', ' ' },
                        StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in queryWords)
                {
                    if (word.Length < 3)
                        continue;

                    if (question.Contains(word))
                        score += 3;

                    if (keywords.Any(k =>
                        k.Equals(word, StringComparison.OrdinalIgnoreCase)))
                    {
                        score += 5;
                    }
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
                    SuggestedQuestions = StaticSuggestions,
                    IsAiGenerated = false
                };
            }

            // =========================================================
            // 3. FINAL FALLBACK
            // =========================================================

            return new ChatbotResponse
            {
                Answer =
                    "I can help with trip planning, destinations, transport, estimated fares, distances, and Trip Analyzer features. Try asking something like: \"Plan a 5 day trip to Rajasthan\" or \"What transport is cheapest from Delhi to Jaipur?\"",
                Category = "Help",
                SuggestedQuestions = StaticSuggestions,
                IsAiGenerated = false
            };
        }

        // =============================================================
        // GEMINI API
        // =============================================================

        private async Task<string?> GetGeminiAnswerAsync(string userMessage)
        {
            var apiKey =
                _config["GEMINI_API_KEY"]
                ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError(
                    "GEMINI_API_KEY is missing.");

                return null;
            }

            var model =
                _config["GEMINI_MODEL"]
                ?? Environment.GetEnvironmentVariable("GEMINI_MODEL")
                ?? "gemini-3.6-flash";

            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

            _logger.LogInformation(
                "Calling Gemini. Model={Model}, UserMessageLength={Length}",
                model,
                userMessage.Length);

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                        new
                        {
                            text =
                                """
                                You are Trip Assistant for Trip Analyzer.

                                Answer the user's question directly.

                                You can help with:
                                - Trip planning
                                - Destination recommendations
                                - Transport
                                - Estimated fares
                                - Estimated distances
                                - Travel duration estimates
                                - Trip Analyzer features
                                - Recommendation scores
                                - General travel questions

                                Rules:
                                1. Be helpful and conversational.
                                2. Keep answers reasonably concise.
                                3. Do not claim live ticket availability.
                                4. Do not invent real-time schedules.
                                5. Do not invent exact current fares.
                                6. Clearly say when information is estimated.
                                7. If the user asks for a trip plan, provide a useful itinerary.
                                8. If the question is unrelated to travel, politely explain that you specialize in travel assistance.
                                """
                        }
                    }
                },

                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new
                            {
                                text = userMessage
                            }
                        }
                    }
                },

                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 1024
                }
            };

            try
            {
                using var client = new HttpClient();

                client.Timeout = TimeSpan.FromSeconds(30);

                // Google Gemini API key
                client.DefaultRequestHeaders.TryAddWithoutValidation(
                    "x-goog-api-key",
                    apiKey);

                var json = JsonSerializer.Serialize(
                    requestBody,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(
                    url,
                    content);

                var responseJson =
                    await response.Content.ReadAsStringAsync();

                // =====================================================
                // LOG ACTUAL ERROR
                // =====================================================

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Gemini API FAILED. HTTP {StatusCode}. Response: {Response}",
                        (int)response.StatusCode,
                        responseJson);

                    return null;
                }

                // =====================================================
                // PARSE RESPONSE
                // =====================================================

                using var document =
                    JsonDocument.Parse(responseJson);

                if (!document.RootElement.TryGetProperty(
                        "candidates",
                        out var candidates))
                {
                    _logger.LogError(
                        "Gemini response does not contain candidates. Response: {Response}",
                        responseJson);

                    return null;
                }

                if (candidates.GetArrayLength() == 0)
                {
                    _logger.LogWarning(
                        "Gemini returned zero candidates.");

                    return null;
                }

                var candidate = candidates[0];

                if (!candidate.TryGetProperty(
                        "content",
                        out var contentElement))
                {
                    _logger.LogError(
                        "Gemini candidate does not contain content. Response: {Response}",
                        responseJson);

                    return null;
                }

                if (!contentElement.TryGetProperty(
                        "parts",
                        out var parts))
                {
                    _logger.LogError(
                        "Gemini content does not contain parts. Response: {Response}",
                        responseJson);

                    return null;
                }

                var answerBuilder = new StringBuilder();

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty(
                            "text",
                            out var textElement))
                    {
                        var text = textElement.GetString();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            answerBuilder.Append(text);
                        }
                    }
                }

                var answer = answerBuilder
                    .ToString()
                    .Trim();

                if (!string.IsNullOrWhiteSpace(answer))
                {
                    _logger.LogInformation(
                        "Gemini successfully generated chatbot response.");

                    return answer;
                }

                _logger.LogWarning(
                    "Gemini returned an empty text response.");

                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Gemini API request timed out.");

                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Network error while calling Gemini API.");

                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Could not parse Gemini response.");

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while calling Gemini.");

                return null;
            }
        }
    }
}