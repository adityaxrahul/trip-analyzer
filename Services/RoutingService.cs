using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TripAnalyzer.Services
{
    public class RoutingService : IRoutingService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<RoutingService> _logger;

        private const string RoutesApiUrl =
            "https://routes.googleapis.com/directions/v2:computeRoutes";

        private const string FieldMask =
            "routes.distanceMeters,routes.duration,routes.polyline.encodedPolyline";

        public RoutingService(IConfiguration config, ILogger<RoutingService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<RouteResult> ComputeRouteAsync(
            double originLat, double originLng,
            double destinationLat, double destinationLng)
        {
            // Always compute the straight-line fallback distance first.
            double straightLine = HaversineKm(originLat, originLng, destinationLat, destinationLng);

            // Read key from IConfiguration first (covers appsettings, user secrets, env vars via host)
            // then fall through to direct env var as a secondary check.
            var apiKey = _config["GOOGLE_ROUTES_API_KEY"]
                      ?? _config["GoogleRoutesApiKey"]
                      ?? _config["Google:RoutesApiKey"]
                      ?? Environment.GetEnvironmentVariable("GOOGLE_ROUTES_API_KEY");

            bool keyLoaded = !string.IsNullOrWhiteSpace(apiKey);

            // Downgrade coordinate logging to Debug to avoid persisting user location in production logs (S-17)
            _logger.LogDebug(
                "[RoutesAPI Debug] GOOGLE_ROUTES_API_KEY loaded: {KeyLoaded} | Origin: ({OriginLat}, {OriginLng}) | Destination: ({DestLat}, {DestLng})",
                keyLoaded, originLat, originLng, destinationLat, destinationLng);

            if (!keyLoaded)
            {
                _logger.LogWarning(
                    "[RoutesAPI Debug] GOOGLE_ROUTES_API_KEY is not configured on the server. Falling back to Haversine ({StraightLine:F1} km).",
                    straightLine);
                return Fallback(straightLine, "GOOGLE_ROUTES_API_KEY not configured on server");
            }

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                client.DefaultRequestHeaders.Add("X-Goog-Api-Key", apiKey);
                client.DefaultRequestHeaders.Add("X-Goog-FieldMask", FieldMask);

                var body = new
                {
                    origin = new
                    {
                        location = new
                        {
                            latLng = new { latitude = originLat, longitude = originLng }
                        }
                    },
                    destination = new
                    {
                        location = new
                        {
                            latLng = new { latitude = destinationLat, longitude = destinationLng }
                        }
                    },
                    travelMode = "DRIVE",
                    routingPreference = "TRAFFIC_UNAWARE",
                    computeAlternativeRoutes = false
                };

                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(RoutesApiUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "[RoutesAPI Debug] Response HTTP Status: {StatusCode} ({Reason})",
                    (int)response.StatusCode, response.ReasonPhrase);

                if (!response.IsSuccessStatusCode)
                {
                    string safeErrorStatus = "HTTP_" + (int)response.StatusCode;
                    string safeErrorMessage = response.ReasonPhrase ?? "HTTP Error";

                    try
                    {
                        using var errorDoc = JsonDocument.Parse(responseBody);
                        if (errorDoc.RootElement.TryGetProperty("error", out var errorEl))
                        {
                            if (errorEl.TryGetProperty("status", out var statusEl))
                            {
                                safeErrorStatus = statusEl.GetString() ?? safeErrorStatus;
                            }
                            if (errorEl.TryGetProperty("message", out var msgEl))
                            {
                                safeErrorMessage = msgEl.GetString() ?? safeErrorMessage;
                            }
                        }
                    }
                    catch
                    {
                        // Non-JSON response body
                    }

                    _logger.LogWarning(
                        "[RoutesAPI Debug] Google Error: Status = {Status}, Message = {ErrorMessage}",
                        safeErrorStatus, safeErrorMessage);

                    return Fallback(straightLine, $"{safeErrorStatus}: {safeErrorMessage}");
                }

                using var doc = JsonDocument.Parse(responseBody);

                if (!doc.RootElement.TryGetProperty("routes", out var routes)
                    || routes.GetArrayLength() == 0)
                {
                    _logger.LogWarning(
                        "[RoutesAPI Debug] Routes API returned 200 OK but 0 routes found.");
                    return Fallback(straightLine, "No driving routes found between specified coordinates");
                }

                var route = routes[0];

                // distanceMeters
                double roadDistanceKm = 0;
                if (route.TryGetProperty("distanceMeters", out var distEl))
                {
                    roadDistanceKm = distEl.GetDouble() / 1000.0;
                }

                // duration — comes as e.g. "1234s" or "4537.123s"
                int durationSeconds = 0;
                if (route.TryGetProperty("duration", out var durEl))
                {
                    var durStr = durEl.GetString() ?? "0s";
                    durationSeconds = ParseDurationSeconds(durStr);
                }

                // encodedPolyline
                string? polyline = null;
                if (route.TryGetProperty("polyline", out var polyEl)
                    && polyEl.TryGetProperty("encodedPolyline", out var encEl))
                {
                    polyline = encEl.GetString();
                }

                // Validate we got real data
                if (roadDistanceKm <= 0 && durationSeconds <= 0)
                {
                    _logger.LogWarning(
                        "Routes API returned zero distance and zero duration. Falling back to Haversine.");
                    return Fallback(straightLine, "Routes API returned 0 distance and 0 duration");
                }

                int durationMinutes = (int)Math.Ceiling(durationSeconds / 60.0);

                _logger.LogInformation(
                    "Routes API success: {RoadKm:F1} km road, {Duration} drive, polyline {HasPoly}",
                    roadDistanceKm, FormatDuration(durationMinutes), polyline != null ? "present" : "absent");

                return new RouteResult
                {
                    RoadDistanceKm  = Math.Round(roadDistanceKm, 1),
                    DurationText    = FormatDuration(durationMinutes),
                    DurationMinutes = durationMinutes,
                    EncodedPolyline = polyline,
                    IsFallback      = false,
                    StraightLineDistanceKm = Math.Round(straightLine, 1),
                    DiagnosticMessage = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Routes API call failed with exception. Falling back to Haversine ({StraightLine:F1} km).",
                    straightLine);
                return Fallback(straightLine, $"Connection Error: {ex.Message}");
            }
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private static RouteResult Fallback(double straightLineKm, string? reason = null) => new RouteResult
        {
            RoadDistanceKm  = null,
            DurationText    = null,
            DurationMinutes = null,
            EncodedPolyline = null,
            IsFallback      = true,
            StraightLineDistanceKm = Math.Round(straightLineKm, 1),
            DiagnosticMessage = reason
        };

        /// <summary>Parses duration strings like "1234s" or "4537.123s".</summary>
        private static int ParseDurationSeconds(string duration)
        {
            var trimmed = duration.TrimEnd('s');
            if (double.TryParse(trimmed, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double seconds))
            {
                return (int)Math.Ceiling(seconds);
            }
            return 0;
        }

        private static string FormatDuration(int totalMinutes)
        {
            if (totalMinutes <= 0) return "N/A";
            int h = totalMinutes / 60;
            int m = totalMinutes % 60;
            return h > 0 ? $"{h}h {m}m" : $"{m}m";
        }

        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }
}
