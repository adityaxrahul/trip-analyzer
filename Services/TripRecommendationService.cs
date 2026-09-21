using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TripAnalyzer.Models;

namespace TripAnalyzer.Services
{
    public class TripRecommendationService : ITripRecommendationService
    {
        public Task<List<TripAnalysis>> AnalyzeTripAsync(Trip trip, List<TransportOption> options)
        {
            if (options == null || !options.Any())
            {
                return Task.FromResult(new List<TripAnalysis>());
            }

            int passengers = Math.Max(1, trip.Passengers);

            // Compute price and duration for each option
            var evaluated = options.Select(opt =>
            {
                decimal calcPrice = opt.BasePricePerPassenger * passengers;
                int calcDuration = opt.DurationMinutes;
                return new
                {
                    Option = opt,
                    CalcPrice = calcPrice,
                    CalcDuration = calcDuration
                };
            }).ToList();

            decimal minPrice = evaluated.Min(e => e.CalcPrice);
            decimal maxPrice = evaluated.Max(e => e.CalcPrice);

            int minDuration = evaluated.Min(e => e.CalcDuration);
            int maxDuration = evaluated.Max(e => e.CalcDuration);

            double maxConvenience = evaluated.Max(e => (e.Option.AvailabilityScore * 0.5) + (e.Option.ComfortScore * 0.5));

            var analyses = new List<TripAnalysis>();

            foreach (var item in evaluated)
            {
                var opt = item.Option;

                // Normalize Cost Score (40% weight): lower price -> higher score
                double costScore = 100.0;
                if (maxPrice > minPrice)
                {
                    double priceRatio = (double)(item.CalcPrice - minPrice) / (double)(maxPrice - minPrice);
                    costScore = Math.Max(0.0, 100.0 * (1.0 - priceRatio));
                }

                // Normalize Duration Score (30% weight): shorter duration -> higher score
                double durationScore = 100.0;
                if (maxDuration > minDuration)
                {
                    double durationRatio = (double)(item.CalcDuration - minDuration) / (double)(maxDuration - minDuration);
                    durationScore = Math.Max(0.0, 100.0 * (1.0 - durationRatio));
                }

                double availabilityScore = Math.Clamp(opt.AvailabilityScore, 0.0, 100.0); // 20% weight
                double comfortScore = Math.Clamp(opt.ComfortScore, 0.0, 100.0); // 10% weight

                double overallScore = (costScore * 0.40) + (durationScore * 0.30) + (availabilityScore * 0.20) + (comfortScore * 0.10);
                overallScore = Math.Round(overallScore, 1);

                bool isCheapest = item.CalcPrice == minPrice;
                bool isFastest = item.CalcDuration == minDuration;
                bool isMostConvenient = ((opt.AvailabilityScore * 0.5) + (opt.ComfortScore * 0.5)) == maxConvenience;

                var analysis = new TripAnalysis
                {
                    TripId = trip.Id,
                    TransportOptionId = opt.Id,
                    TransportOption = opt,
                    CalculatedPrice = item.CalcPrice,
                    CalculatedDurationMinutes = item.CalcDuration,
                    CostScore = Math.Round(costScore, 1),
                    DurationScore = Math.Round(durationScore, 1),
                    AvailabilityScore = Math.Round(availabilityScore, 1),
                    ComfortScore = Math.Round(comfortScore, 1),
                    OverallScore = overallScore,
                    IsCheapest = isCheapest,
                    IsFastest = isFastest,
                    IsMostConvenient = isMostConvenient,
                    IsRecommended = false // Set after comparing overall scores
                };

                analyses.Add(analysis);
            }

            // Identify the top overall score for IsRecommended
            if (analyses.Any())
            {
                var winner = analyses.OrderByDescending(a => a.OverallScore).ThenBy(a => a.CalculatedPrice).First();
                winner.IsRecommended = true;

                // Build transparent human-readable explanations for all options
                foreach (var a in analyses)
                {
                    a.ReasoningText = BuildReasoningText(a, winner, minPrice, minDuration);
                }
            }

            return Task.FromResult(analyses);
        }

        private static string BuildReasoningText(TripAnalysis a, TripAnalysis winner, decimal minPrice, int minDuration)
        {
            var opt = a.TransportOption;
            string mode = opt?.Mode ?? "Transport";
            int hours = a.CalculatedDurationMinutes / 60;
            int mins = a.CalculatedDurationMinutes % 60;
            string durationStr = hours > 0 ? $"{hours}h {mins}m" : $"{mins}m";

            if (a.IsRecommended)
            {
                if (a.IsCheapest && a.IsFastest)
                {
                    return $"{mode} is both the cheapest (₹{a.CalculatedPrice:N0}) and fastest ({durationStr}) mode of travel for this trip, making it the undisputed winner.";
                }
                if (a.IsCheapest)
                {
                    return $"{mode} offers the best budget value (₹{a.CalculatedPrice:N0}) while keeping travel time reasonable at {durationStr}.";
                }
                if (a.IsFastest)
                {
                    return $"{mode} delivers the fastest journey time ({durationStr}) with premium comfort, justifying the higher fare.";
                }
                return $"{mode} offers the strongest overall balance of cost (₹{a.CalculatedPrice:N0}), duration ({durationStr}), and onboard comfort for this route.";
            }
            else
            {
                if (a.IsCheapest)
                {
                    return $"{mode} is the most economical choice at ₹{a.CalculatedPrice:N0}, though it takes longer ({durationStr}) than higher-scoring options.";
                }
                if (a.IsFastest)
                {
                    return $"{mode} cuts travel time down to {durationStr}, but comes at a higher fare of ₹{a.CalculatedPrice:N0}.";
                }
                return $"{mode} provides a valid alternative ({durationStr}, ₹{a.CalculatedPrice:N0}), but scores lower on cost-to-time efficiency compared to {winner.TransportOption?.Mode ?? "the recommended choice"}.";
            }
        }
    }
}
