using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TripAnalyzer.Data;
using TripAnalyzer.Models;
using TripAnalyzer.Services;
using TripAnalyzer.ViewModels;

namespace TripAnalyzer.Controllers
{
    public class TripController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ITripRecommendationService _recommendationService;
        private readonly IDistanceCalculationService _distanceService;
        private readonly IFareCalculationService _fareService;
        private readonly IRoutingService _routingService;
        private readonly IConfiguration _config;
        private readonly ILogger<TripController> _logger;

        public TripController(
            ApplicationDbContext db,
            ITripRecommendationService recommendationService,
            IDistanceCalculationService distanceService,
            IFareCalculationService fareService,
            IRoutingService routingService,
            IConfiguration config,
            ILogger<TripController> logger)
        {
            _db = db;
            _recommendationService = recommendationService;
            _distanceService = distanceService;
            _fareService = fareService;
            _routingService = routingService;
            _config = config;
            _logger = logger;
        }

        [Authorize]
        [HttpGet]
        [EnableRateLimiting("trip")]
        public async Task<IActionResult> Analyze(string source, string destination, DateTime? travelDate, int passengers = 1)
        {
            // Server-side length clamp for GET query-string inputs (S-20)
            if (!string.IsNullOrWhiteSpace(source) && source.Length > 100)
                source = source.Substring(0, 100);
            if (!string.IsNullOrWhiteSpace(destination) && destination.Length > 100)
                destination = destination.Substring(0, 100);

            var searchModel = new TripSearchViewModel
            {
                Source = string.IsNullOrWhiteSpace(source) ? "Rajkot" : source.Trim(),
                Destination = string.IsNullOrWhiteSpace(destination) ? "Mumbai" : destination.Trim(),
                TravelDate = travelDate ?? DateTime.UtcNow.AddDays(1),
                Passengers = passengers < 1 ? 1 : passengers
            };

            return await PerformAnalysisAsync(searchModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("trip")]
        public async Task<IActionResult> Analyze(TripSearchViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/Index.cshtml", model);
            }

            return await PerformAnalysisAsync(model);
        }

        private async Task<IActionResult> PerformAnalysisAsync(TripSearchViewModel model)
        {
            string source = model.Source.Trim();
            string destination = model.Destination.Trim();
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Check if source and destination exist in the database (active locations: City or District)
            bool srcExists = await _db.Cities.AnyAsync(c => c.IsActive && EF.Functions.ILike(c.Name, source)) ||
                             await _db.Districts.AnyAsync(d => d.IsActive && EF.Functions.ILike(d.Name, source));

            bool destExists = await _db.Cities.AnyAsync(c => c.IsActive && EF.Functions.ILike(c.Name, destination)) ||
                              await _db.Districts.AnyAsync(d => d.IsActive && EF.Functions.ILike(d.Name, destination));

            if (!srcExists || !destExists)
            {
                ViewBag.SourceAvailable = srcExists;
                ViewBag.DestinationAvailable = destExists;
                ViewBag.SourceQuery = source;
                ViewBag.DestinationQuery = destination;

                var supportedCities = await _db.Cities.Where(c => c.IsActive).Select(c => c.Name).ToListAsync();
                var supportedDistricts = await _db.Districts.Where(d => d.IsActive).Select(d => d.Name).ToListAsync();
                ViewBag.SupportedLocations = supportedCities.Concat(supportedDistricts).Distinct().OrderBy(name => name).ToList();

                return View("LocationNotAvailable", model);
            }

            if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.SourceAvailable = true;
                ViewBag.DestinationAvailable = true;
                ViewBag.SourceQuery = source;
                ViewBag.DestinationQuery = destination;
                ViewBag.CustomReason = "Origin and destination cannot be identical. Please choose two distinct locations.";

                var supportedCities = await _db.Cities.Where(c => c.IsActive).Select(c => c.Name).ToListAsync();
                var supportedDistricts = await _db.Districts.Where(d => d.IsActive).Select(d => d.Name).ToListAsync();
                ViewBag.SupportedLocations = supportedCities.Concat(supportedDistricts).Distinct().OrderBy(name => name).ToList();

                return View("LocationNotAvailable", model);
            }

            // ── Resolve coordinates for source and destination ────────────
            var srcCoords  = await ResolveCoordinatesAsync(source);
            var destCoords = await ResolveCoordinatesAsync(destination);

            // ── Call Routes API (server-side, key never leaves backend) ──
            RouteResult routeResult;
            if (srcCoords.HasValue && destCoords.HasValue)
            {
                routeResult = await _routingService.ComputeRouteAsync(
                    srcCoords.Value.Lat,  srcCoords.Value.Lng,
                    destCoords.Value.Lat, destCoords.Value.Lng);
            }
            else
            {
                // No coordinates in DB — use straight-line only
                double sl = await _distanceService.CalculateDistanceByNameAsync(source, destination);
                routeResult = new RouteResult { IsFallback = true, StraightLineDistanceKm = sl };
            }

            // Road distance for Bus/Car; straight-line for Train/Flight
            double roadDistKm      = routeResult.RoadDistanceKm ?? routeResult.StraightLineDistanceKm;
            double straightLineKm  = routeResult.StraightLineDistanceKm;

            // Fetch matching transport options (case insensitive)
            var options = await _db.TransportOptions
                .Where(t => t.IsActive &&
                            EF.Functions.ILike(t.Source, source) &&
                            EF.Functions.ILike(t.Destination, destination))
                .ToListAsync();

            // If no exact match exists, generate dynamic estimated options
            if (!options.Any())
            {
                options = await GenerateDynamicOptionsAsync(source, destination, roadDistKm, routeResult.DurationMinutes, straightLineKm, model.Passengers);
            }

            // Store road distance when available; fall back to straight-line (B-04 fix)
            double distanceKm = routeResult.RoadDistanceKm.HasValue && routeResult.RoadDistanceKm.Value > 0
                ? routeResult.RoadDistanceKm.Value
                : straightLineKm;

            var utcTravelDate = model.TravelDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(model.TravelDate, DateTimeKind.Utc)
                : model.TravelDate.ToUniversalTime();

            // Create Trip record
            var trip = new Trip
            {
                UserId = userId,
                Source = source,
                Destination = destination,
                TravelDate = utcTravelDate,
                Passengers = model.Passengers,
                DistanceKm = distanceKm,
                CreatedAt = DateTime.UtcNow
            };

            _db.Trips.Add(trip);
            await _db.SaveChangesAsync();

            // Run recommendation scoring engine
            var analyses = await _recommendationService.AnalyzeTripAsync(trip, options);

            foreach (var a in analyses)
            {
                a.TripId = trip.Id;
            }

            await _db.TripAnalyses.AddRangeAsync(analyses);
            await _db.SaveChangesAsync();

            bool isSaved = false;
            int? savedTripId = null;

            if (!string.IsNullOrEmpty(userId))
            {
                var saved = await _db.SavedTrips.FirstOrDefaultAsync(s => s.UserId == userId && s.TripId == trip.Id);
                if (saved != null)
                {
                    isSaved = true;
                    savedTripId = saved.Id;
                }
            }

            // Browser-safe Maps key (never the Routes server key)
            string? mapsKey = _config["GOOGLE_MAPS_API_KEY"]
                           ?? Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");

            var trainOpt = options.FirstOrDefault(o => o.Mode.Equals("Train", StringComparison.OrdinalIgnoreCase));
            var flightOpt = options.FirstOrDefault(o => o.Mode.Equals("Flight", StringComparison.OrdinalIgnoreCase));
            var busOpt = options.FirstOrDefault(o => o.Mode.Equals("Bus", StringComparison.OrdinalIgnoreCase));
            var carOpt = options.FirstOrDefault(o => o.Mode.Equals("Car", StringComparison.OrdinalIgnoreCase));

            var viewModel = new TripAnalysisResultViewModel
            {
                Trip = trip,
                Analyses = analyses,
                RecommendedOption    = analyses.FirstOrDefault(a => a.IsRecommended),
                CheapestOption       = analyses.FirstOrDefault(a => a.IsCheapest),
                FastestOption        = analyses.FirstOrDefault(a => a.IsFastest),
                MostConvenientOption = analyses.FirstOrDefault(a => a.IsMostConvenient),
                IsSaved     = isSaved,
                SavedTripId = savedTripId,

                // Route / Map fields
                SourceLatitude       = srcCoords?.Lat  ?? 0,
                SourceLongitude      = srcCoords?.Lng  ?? 0,
                DestinationLatitude  = destCoords?.Lat ?? 0,
                DestinationLongitude = destCoords?.Lng ?? 0,
                StraightLineDistanceKm = Math.Round(straightLineKm, 1),

                RoadDistanceKm   = routeResult.RoadDistanceKm,
                RoadDurationMinutes = routeResult.DurationMinutes,
                RoadDurationText = routeResult.DurationText,

                TrainDistanceKm = trainOpt?.DistanceKm,
                TrainDurationMinutes = trainOpt?.DurationMinutes,
                TrainDurationText = trainOpt != null ? FormatDuration(trainOpt.DurationMinutes) : null,

                FlightDistanceKm = flightOpt?.DistanceKm,
                FlightDurationMinutes = flightOpt?.DurationMinutes,
                FlightDurationText = flightOpt != null ? FormatDuration(flightOpt.DurationMinutes) : null,

                BusDistanceKm = busOpt?.DistanceKm,
                BusDurationMinutes = busOpt?.DurationMinutes,
                BusDurationText = busOpt != null ? FormatDuration(busOpt.DurationMinutes) : null,

                CarDistanceKm = carOpt?.DistanceKm,
                CarDurationMinutes = carOpt?.DurationMinutes,
                CarDurationText = carOpt != null ? FormatDuration(carOpt.DurationMinutes) : null,

                EncodedPolyline  = routeResult.EncodedPolyline,
                IsRouteFallback  = routeResult.IsFallback,
                RouteDiagnosticMessage = routeResult.DiagnosticMessage,
                GoogleMapsApiKey = mapsKey
            };

            return View("Analyze", viewModel);
        }

        // ── Coordinate resolver (City → District fallback) ───────────────
        private async Task<(double Lat, double Lng)?> ResolveCoordinatesAsync(string name)
        {
            var city = await _db.Cities.FirstOrDefaultAsync(
                c => c.IsActive && EF.Functions.ILike(c.Name, name));
            if (city != null) return (city.Latitude, city.Longitude);

            var district = await _db.Districts.FirstOrDefaultAsync(
                d => d.IsActive && EF.Functions.ILike(d.Name, name));
            if (district != null) return (district.Latitude, district.Longitude);

            return null;
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTrip(int tripId, string? notes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var trip = await _db.Trips.FindAsync(tripId);
            if (trip == null) return NotFound();

            var existing = await _db.SavedTrips.FirstOrDefaultAsync(s => s.UserId == userId && s.TripId == tripId);
            if (existing != null)
            {
                _db.SavedTrips.Remove(existing);
                await _db.SaveChangesAsync();
                TempData["InfoMessage"] = "Trip removed from your saved list.";
            }
            else
            {
                var saved = new SavedTrip
                {
                    UserId = userId,
                    TripId = tripId,
                    Notes = notes,
                    SavedAt = DateTime.UtcNow
                };
                _db.SavedTrips.Add(saved);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Trip saved to your favorites!";
            }

            return RedirectToAction(nameof(Analyze), new { source = trip.Source, destination = trip.Destination, travelDate = trip.TravelDate, passengers = trip.Passengers });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var historyTrips = await _db.Trips
                .Include(t => t.Analyses)
                    .ThenInclude(a => a.TransportOption)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var savedTrips = await _db.SavedTrips
                .Include(s => s.Trip!)
                    .ThenInclude(t => t.Analyses)
                        .ThenInclude(a => a.TransportOption)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SavedAt)
                .ToListAsync();

            string topDest = historyTrips.GroupBy(t => t.Destination)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? "N/A";

            string topMode = historyTrips.SelectMany(t => t.Analyses)
                .Where(a => a.IsRecommended && a.TransportOption != null)
                .GroupBy(a => a.TransportOption!.Mode)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? "Train";

            var viewModel = new UserHistoryViewModel
            {
                HistoryTrips = historyTrips,
                SavedTrips = savedTrips,
                TotalSearches = historyTrips.Count,
                MostSearchedDestination = topDest,
                MostSelectedTransport = topMode
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrip(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (trip != null)
            {
                _db.Trips.Remove(trip);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Trip search deleted from your history.";
            }

            return RedirectToAction(nameof(History));
        }

        private async Task<List<TransportOption>> GenerateDynamicOptionsAsync(
            string source, string destination,
            double roadDistKm = 0, int? roadDurationMinutes = null, double straightLineKm = 0, int passengers = 1)
        {
            // If not supplied, fall back to Haversine
            if (straightLineKm <= 0)
                straightLineKm = await _distanceService.CalculateDistanceByNameAsync(source, destination);
            if (roadDistKm <= 0)
                roadDistKm = straightLineKm;

            // 1. Compute mode-specific distance, duration, and fare
            var trainEst  = await _fareService.EstimateTrainAsync(roadDistKm, straightLineKm, passengers);
            var flightEst = await _fareService.EstimateFlightAsync(straightLineKm);
            var busEst    = await _fareService.EstimateBusAsync(roadDistKm, roadDurationMinutes, straightLineKm);
            var carEst    = await _fareService.EstimateCarAsync(roadDistKm, roadDurationMinutes, straightLineKm);

            // 2. Internal debug logging (Mode | DistanceKm | DurationMinutes | CalculationSource)
            _logger.LogDebug("[Transport Calc] CAR | {Distance} | {Duration} | {Source}",
                carEst.DistanceKm, carEst.DurationMinutes, carEst.CalculationSource);
            _logger.LogDebug("[Transport Calc] BUS | {Distance} | {Duration} | {Source}",
                busEst.DistanceKm, busEst.DurationMinutes, busEst.CalculationSource);
            _logger.LogDebug("[Transport Calc] TRAIN | {Distance} | {Duration} | {Source}",
                trainEst.DistanceKm, trainEst.DurationMinutes, trainEst.CalculationSource);
            _logger.LogDebug("[Transport Calc] FLIGHT | {Distance} | {Duration} | {Source}",
                flightEst.DistanceKm, flightEst.DurationMinutes, flightEst.CalculationSource);

            var options = new List<TransportOption>
            {
                new TransportOption
                {
                    Source = source,
                    Destination = destination,
                    Mode = "Train",
                    CarrierName = "Indian Railways Express",
                    BasePricePerPassenger = trainEst.BasePricePerPassenger,
                    DurationMinutes = trainEst.DurationMinutes,
                    DistanceKm = trainEst.DistanceKm,
                    AvailabilityScore = 85.0,
                    ComfortScore = 80.0,
                    EcoRating = 4.7,
                    DepartureTime = "Benchmark / Estimated",
                    ArrivalTime = "Estimated",
                    Amenities = "Sleeper / 3AC Rail Benchmark",
                    IsActive = true,
                    IsEstimatedData = true
                },
                new TransportOption
                {
                    Source = source,
                    Destination = destination,
                    Mode = "Bus",
                    CarrierName = "Intercity Express Bus",
                    BasePricePerPassenger = busEst.BasePricePerPassenger,
                    DurationMinutes = busEst.DurationMinutes,
                    DistanceKm = busEst.DistanceKm,
                    AvailabilityScore = 90.0,
                    ComfortScore = 72.0,
                    EcoRating = 3.6,
                    DepartureTime = "Benchmark / Estimated",
                    ArrivalTime = "Estimated",
                    Amenities = "AC Sleeper / Seater Bus",
                    IsActive = true,
                    IsEstimatedData = true
                },
                new TransportOption
                {
                    Source = source,
                    Destination = destination,
                    Mode = "Flight",
                    CarrierName = "Commercial Airline",
                    BasePricePerPassenger = flightEst.BasePricePerPassenger,
                    DurationMinutes = flightEst.DurationMinutes,
                    DistanceKm = flightEst.DistanceKm,
                    AvailabilityScore = 80.0,
                    ComfortScore = 90.0,
                    EcoRating = 2.4,
                    DepartureTime = "Benchmark / Estimated",
                    ArrivalTime = "Estimated",
                    Amenities = "Economy Cabin + Airport Handling",
                    IsActive = true,
                    IsEstimatedData = true
                },
                new TransportOption
                {
                    Source = source,
                    Destination = destination,
                    Mode = "Car",
                    CarrierName = "Private Car / Taxi",
                    BasePricePerPassenger = carEst.BasePricePerPassenger,
                    DurationMinutes = carEst.DurationMinutes,
                    DistanceKm = carEst.DistanceKm,
                    AvailabilityScore = 75.0,
                    ComfortScore = 85.0,
                    EcoRating = 3.0,
                    DepartureTime = "On-Demand / Flexible",
                    ArrivalTime = "Estimated",
                    Amenities = "Door-to-Door Driving Route",
                    IsActive = true,
                    IsEstimatedData = carEst.IsBenchmark
                }
            };

            return options;
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
