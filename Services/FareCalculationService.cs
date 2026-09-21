using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TripAnalyzer.Data;
using TripAnalyzer.Models;

namespace TripAnalyzer.Services
{
    public class FareCalculationService : IFareCalculationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public FareCalculationService(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<decimal> CalculateFareAsync(string mode, double distanceKm, int passengers = 1)
        {
            var config = await _db.FareConfigurations
                .FirstOrDefaultAsync(f => f.IsActive && f.Mode.ToLower() == mode.ToLower());

            decimal farePerPassenger;

            if (config != null)
            {
                farePerPassenger = config.BaseFare + ((decimal)distanceKm * config.PerKmRate);
                if (farePerPassenger < config.MinimumFare)
                {
                    farePerPassenger = config.MinimumFare;
                }
            }
            else
            {
                // Fallback rules if no config is available in DB
                switch (mode.ToLower())
                {
                    case "train":
                        farePerPassenger = Math.Max(250m, (decimal)(distanceKm * 1.3));
                        break;
                    case "bus":
                        farePerPassenger = Math.Max(300m, (decimal)(distanceKm * 1.5));
                        break;
                    case "flight":
                        farePerPassenger = Math.Max(2500m, (decimal)(distanceKm * 4.8));
                        break;
                    case "car":
                        farePerPassenger = Math.Max(600m, (decimal)(distanceKm * 3.2));
                        break;
                    default:
                        farePerPassenger = Math.Max(100m, (decimal)(distanceKm * 1.0));
                        break;
                }
            }

            return Math.Round(farePerPassenger, 0);
        }

        public Task<int> CalculateDurationMinutesAsync(string mode, double distanceKm)
        {
            int durationMinutes;
            switch (mode.ToLower())
            {
                case "train":
                    double trainSpeed = GetConfigDouble("TravelBenchmarks:Train:AverageSpeedKmh", 55.0);
                    double trainBuffer = GetConfigDouble("TravelBenchmarks:Train:StopBufferHours", 1.5);
                    durationMinutes = Math.Max(60, (int)Math.Round((distanceKm / trainSpeed * 60) + (trainBuffer * 60)));
                    break;
                case "bus":
                    double busSpeed = GetConfigDouble("TravelBenchmarks:Bus:AverageSpeedKmh", 50.0);
                    double busBuffer = GetConfigDouble("TravelBenchmarks:Bus:StopBufferHours", 0.5);
                    durationMinutes = Math.Max(60, (int)Math.Round((distanceKm / busSpeed * 60) + (busBuffer * 60)));
                    break;
                case "flight":
                    double flightSpeed = GetConfigDouble("TravelBenchmarks:Flight:AverageSpeedKmh", 750.0);
                    double flightBuffer = GetConfigDouble("TravelBenchmarks:Flight:BoardingBufferHours", 1.75);
                    durationMinutes = Math.Max(60, (int)Math.Round((distanceKm / flightSpeed * 60) + (flightBuffer * 60)));
                    break;
                case "car":
                    double carSpeed = GetConfigDouble("TravelBenchmarks:Car:DefaultSpeedKmh", 70.0);
                    durationMinutes = Math.Max(45, (int)Math.Round(distanceKm / carSpeed * 60));
                    break;
                default:
                    durationMinutes = Math.Max(60, (int)(distanceKm / 60.0 * 60));
                    break;
            }
            return Task.FromResult(durationMinutes);
        }

        public async Task<ModeEstimateResult> EstimateTrainAsync(double roadDistanceKm, double straightLineKm, int passengers = 1)
        {
            double speed = GetConfigDouble("TravelBenchmarks:Train:AverageSpeedKmh", 55.0);
            double factor = GetConfigDouble("TravelBenchmarks:Train:DistanceFactor", 1.05);
            double buffer = GetConfigDouble("TravelBenchmarks:Train:StopBufferHours", 1.5);

            // Rail track distance model: road distance * factor (or straight-line * 1.30 if road not available)
            double distance = roadDistanceKm > 0
                ? Math.Round(roadDistanceKm * factor, 1)
                : Math.Round(straightLineKm * 1.30, 1);

            double travelHours = (distance / speed) + buffer;
            int durationMinutes = Math.Max(45, (int)Math.Round(travelHours * 60));
            int pax = passengers < 1 ? 1 : passengers;
            decimal basePrice = await CalculateFareAsync("Train", distance, pax);

            return new ModeEstimateResult
            {
                Mode = "Train",
                DistanceKm = distance,
                DurationMinutes = durationMinutes,
                DurationText = FormatDuration(durationMinutes),
                BasePricePerPassenger = basePrice,
                CalculationSource = $"Rail Benchmark ({speed:F0} km/h + {buffer:F1}h buffer)",
                DistanceLabel = "Estimated Rail Distance",
                DurationLabel = "Estimated Train Duration",
                IsBenchmark = true
            };
        }

        public async Task<ModeEstimateResult> EstimateFlightAsync(double straightLineKm)
        {
            double speed = GetConfigDouble("TravelBenchmarks:Flight:AverageSpeedKmh", 750.0);
            double buffer = GetConfigDouble("TravelBenchmarks:Flight:BoardingBufferHours", 1.75);

            double distance = Math.Round(straightLineKm, 1);
            double travelHours = (distance / speed) + buffer;
            int durationMinutes = Math.Max(60, (int)Math.Round(travelHours * 60));
            decimal basePrice = await CalculateFareAsync("Flight", distance, 1);

            return new ModeEstimateResult
            {
                Mode = "Flight",
                DistanceKm = distance,
                DurationMinutes = durationMinutes,
                DurationText = FormatDuration(durationMinutes),
                BasePricePerPassenger = basePrice,
                CalculationSource = $"Flight Benchmark ({speed:F0} km/h + {buffer:F2}h airport buffer)",
                DistanceLabel = "Straight-Line Distance",
                DurationLabel = "Estimated Flight Duration",
                IsBenchmark = true
            };
        }

        public async Task<ModeEstimateResult> EstimateBusAsync(double roadDistanceKm, int? googleRoadDurationMinutes, double straightLineKm)
        {
            double speed = GetConfigDouble("TravelBenchmarks:Bus:AverageSpeedKmh", 50.0);
            double multiplier = GetConfigDouble("TravelBenchmarks:Bus:RoadDurationMultiplier", 1.15);
            double buffer = GetConfigDouble("TravelBenchmarks:Bus:StopBufferHours", 0.5);

            double distance = roadDistanceKm > 0
                ? Math.Round(roadDistanceKm, 1)
                : Math.Round(straightLineKm * 1.20, 1);

            int durationMinutes;
            string source;

            if (googleRoadDurationMinutes.HasValue && googleRoadDurationMinutes.Value > 0)
            {
                durationMinutes = Math.Max(45, (int)Math.Round((googleRoadDurationMinutes.Value * multiplier) + (buffer * 60)));
                source = $"Google road duration ({FormatDuration(googleRoadDurationMinutes.Value)}) + bus adjustment ({multiplier:F2}x + {buffer:F1}h buffer)";
            }
            else
            {
                double travelHours = (distance / speed) + buffer;
                durationMinutes = Math.Max(45, (int)Math.Round(travelHours * 60));
                source = $"Bus Benchmark ({speed:F0} km/h + {buffer:F1}h buffer)";
            }

            decimal basePrice = await CalculateFareAsync("Bus", distance, 1);

            return new ModeEstimateResult
            {
                Mode = "Bus",
                DistanceKm = distance,
                DurationMinutes = durationMinutes,
                DurationText = FormatDuration(durationMinutes),
                BasePricePerPassenger = basePrice,
                CalculationSource = source,
                DistanceLabel = "Road Distance",
                DurationLabel = "Estimated Bus Duration",
                IsBenchmark = true
            };
        }

        public async Task<ModeEstimateResult> EstimateCarAsync(double roadDistanceKm, int? googleRoadDurationMinutes, double straightLineKm)
        {
            double speed = GetConfigDouble("TravelBenchmarks:Car:DefaultSpeedKmh", 70.0);

            double distance = roadDistanceKm > 0
                ? Math.Round(roadDistanceKm, 1)
                : Math.Round(straightLineKm * 1.15, 1);

            int durationMinutes;
            string source;
            bool isBenchmark = false;

            if (googleRoadDurationMinutes.HasValue && googleRoadDurationMinutes.Value > 0)
            {
                durationMinutes = googleRoadDurationMinutes.Value;
                source = "Google Routes API";
            }
            else
            {
                double travelHours = distance / speed;
                durationMinutes = Math.Max(30, (int)Math.Round(travelHours * 60));
                source = $"Road Fallback ({speed:F0} km/h)";
                isBenchmark = true;
            }

            decimal basePrice = await CalculateFareAsync("Car", distance, 1);

            return new ModeEstimateResult
            {
                Mode = "Car",
                DistanceKm = distance,
                DurationMinutes = durationMinutes,
                DurationText = FormatDuration(durationMinutes),
                BasePricePerPassenger = basePrice,
                CalculationSource = source,
                DistanceLabel = "Road Distance",
                DurationLabel = isBenchmark ? "Estimated Road Duration" : "Google Estimated Road Time",
                IsBenchmark = isBenchmark
            };
        }

        private double GetConfigDouble(string key, double defaultValue)
        {
            var valStr = _config[key];
            if (!string.IsNullOrWhiteSpace(valStr) &&
                double.TryParse(valStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double parsed))
            {
                return parsed;
            }
            return defaultValue;
        }

        private static string FormatDuration(int totalMinutes)
        {
            if (totalMinutes <= 0) return "N/A";
            int h = totalMinutes / 60;
            int m = totalMinutes % 60;
            return h > 0 ? $"{h}h {m}m" : $"{m}m";
        }
    }
}
