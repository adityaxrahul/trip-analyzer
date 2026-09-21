using System;

namespace TripAnalyzer.Models
{
    public class TransportOption
    {
        public int Id { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty; // Train, Bus, Flight, Car
        public string CarrierName { get; set; } = string.Empty;
        public decimal BasePricePerPassenger { get; set; }
        public int DurationMinutes { get; set; }
        public double DistanceKm { get; set; }
        public double AvailabilityScore { get; set; } = 80.0; // 0 - 100
        public double ComfortScore { get; set; } = 75.0; // 0 - 100
        public double EcoRating { get; set; } = 4.0; // 1 - 5
        public string DepartureTime { get; set; } = "08:00 AM";
        public string ArrivalTime { get; set; } = "08:00 PM";
        public string Amenities { get; set; } = "AC, Charging Port, Reclining Seats";
        public bool IsActive { get; set; } = true;
        public bool IsEstimatedData { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
