using System.Threading.Tasks;

namespace TripAnalyzer.Services
{
    public class RouteResult
    {
        /// <summary>Road distance in kilometres. Null if routing failed.</summary>
        public double? RoadDistanceKm { get; set; }

        /// <summary>Driving duration as a human-readable string, e.g. "3h 45m".</summary>
        public string? DurationText { get; set; }

        /// <summary>Driving duration in minutes (for fare/recommendation calculations).</summary>
        public int? DurationMinutes { get; set; }

        /// <summary>Google-encoded polyline for the primary route. Null if routing failed.</summary>
        public string? EncodedPolyline { get; set; }

        /// <summary>True when the Haversine straight-line distance was used instead of a road route.</summary>
        public bool IsFallback { get; set; } = false;

        /// <summary>Straight-line (Haversine) distance in kilometres, always populated.</summary>
        public double StraightLineDistanceKm { get; set; }

        /// <summary>Safe diagnostic message or reason for fallback.</summary>
        public string? DiagnosticMessage { get; set; }
    }

    public interface IRoutingService
    {
        /// <summary>
        /// Computes the driving route between two geographic coordinates.
        /// Falls back to Haversine straight-line distance when the Routes API
        /// is unavailable, returns no route, or the API key is missing.
        /// </summary>
        Task<RouteResult> ComputeRouteAsync(
            double originLat, double originLng,
            double destinationLat, double destinationLng);
    }
}
