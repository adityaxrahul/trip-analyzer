using System;

namespace TripAnalyzer.Models
{
    public class TripAnalysis
    {
        public int Id { get; set; }

        public int TripId { get; set; }
        public virtual Trip? Trip { get; set; }

        public int TransportOptionId { get; set; }
        public virtual TransportOption? TransportOption { get; set; }

        public decimal CalculatedPrice { get; set; }
        public int CalculatedDurationMinutes { get; set; }
        
        public double OverallScore { get; set; }
        public double CostScore { get; set; }
        public double DurationScore { get; set; }
        public double AvailabilityScore { get; set; }
        public double ComfortScore { get; set; }

        public bool IsRecommended { get; set; }
        public bool IsCheapest { get; set; }
        public bool IsFastest { get; set; }
        public bool IsMostConvenient { get; set; }

        public string ReasoningText { get; set; } = string.Empty;
    }
}
