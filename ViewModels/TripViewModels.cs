using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TripAnalyzer.Models;

namespace TripAnalyzer.ViewModels
{
    public class TripSearchViewModel
    {
        [Required(ErrorMessage = "Please enter your starting location.")]
        [StringLength(100, ErrorMessage = "Source location must not exceed 100 characters.")]
        [Display(Name = "Current Location / Source")]
        public string Source { get; set; } = "Rajkot";

        [Required(ErrorMessage = "Please enter your destination.")]
        [StringLength(100, ErrorMessage = "Destination must not exceed 100 characters.")]
        [Display(Name = "Destination")]
        public string Destination { get; set; } = "Mumbai";

        [Required(ErrorMessage = "Please select a travel date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Travel Date")]
        public DateTime TravelDate { get; set; } = DateTime.UtcNow.AddDays(1);

        [Range(1, 10, ErrorMessage = "Passengers must be between 1 and 10.")]
        [Display(Name = "Passengers")]
        public int Passengers { get; set; } = 1;

        public double? UserLatitude { get; set; }
        public double? UserLongitude { get; set; }
    }

    public class TripAnalysisResultViewModel
    {
        public Trip Trip { get; set; } = new Trip();
        public List<TripAnalysis> Analyses { get; set; } = new List<TripAnalysis>();

        public TripAnalysis? RecommendedOption { get; set; }
        public TripAnalysis? CheapestOption { get; set; }
        public TripAnalysis? FastestOption { get; set; }
        public TripAnalysis? MostConvenientOption { get; set; }

        public bool IsSaved { get; set; }
        public int? SavedTripId { get; set; }
        public string? UserNotes { get; set; }

        // ── Route / Map data ────────────────────────────────────────────
        public double SourceLatitude { get; set; }
        public double SourceLongitude { get; set; }
        public double DestinationLatitude { get; set; }
        public double DestinationLongitude { get; set; }

        /// <summary>Straight-line (air) distance in km.</summary>
        public double StraightLineDistanceKm { get; set; }

        /// <summary>Road distance in km (null = routing unavailable).</summary>
        public double? RoadDistanceKm { get; set; }

        /// <summary>Road driving duration in minutes.</summary>
        public int? RoadDurationMinutes { get; set; }

        /// <summary>Human-readable driving duration string, e.g. "3h 45m".</summary>
        public string? RoadDurationText { get; set; }

        /// <summary>Train estimated rail distance in km.</summary>
        public double? TrainDistanceKm { get; set; }

        /// <summary>Train estimated duration in minutes.</summary>
        public int? TrainDurationMinutes { get; set; }

        /// <summary>Train estimated duration text, e.g. "33h 38m".</summary>
        public string? TrainDurationText { get; set; }

        /// <summary>Flight air distance in km.</summary>
        public double? FlightDistanceKm { get; set; }

        /// <summary>Flight estimated duration in minutes.</summary>
        public int? FlightDurationMinutes { get; set; }

        /// <summary>Flight estimated duration text, e.g. "3h 16m".</summary>
        public string? FlightDurationText { get; set; }

        /// <summary>Bus road distance in km.</summary>
        public double? BusDistanceKm { get; set; }

        /// <summary>Bus estimated duration in minutes.</summary>
        public int? BusDurationMinutes { get; set; }

        /// <summary>Bus estimated duration text, e.g. "31h 12m".</summary>
        public string? BusDurationText { get; set; }

        /// <summary>Car road distance in km.</summary>
        public double? CarDistanceKm { get; set; }

        /// <summary>Car duration in minutes.</summary>
        public int? CarDurationMinutes { get; set; }

        /// <summary>Car duration text, e.g. "26h 42m".</summary>
        public string? CarDurationText { get; set; }

        /// <summary>Google-encoded polyline for map drawing (null = fallback).</summary>
        public string? EncodedPolyline { get; set; }

        /// <summary>True when only Haversine distance is available.</summary>
        public bool IsRouteFallback { get; set; } = true;

        /// <summary>
        /// Browser-restricted Maps/Places API key injected from server config.
        /// Safe to render in HTML — never the Routes server key.
        /// </summary>
        public string? GoogleMapsApiKey { get; set; }

        /// <summary>Safe diagnostic message explaining fallback reason if applicable.</summary>
        public string? RouteDiagnosticMessage { get; set; }
    }

    public class UserHistoryViewModel
    {
        public List<Trip> HistoryTrips { get; set; } = new List<Trip>();
        public List<SavedTrip> SavedTrips { get; set; } = new List<SavedTrip>();
        
        public int TotalSearches { get; set; }
        public string MostSearchedDestination { get; set; } = "N/A";
        public string MostSelectedTransport { get; set; } = "N/A";
    }
}
