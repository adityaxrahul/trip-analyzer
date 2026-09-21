using System;
using System.Collections.Generic;

namespace TripAnalyzer.Models
{
    public class Trip
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime TravelDate { get; set; } = DateTime.UtcNow.Date;
        public int Passengers { get; set; } = 1;
        public double DistanceKm { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<TripAnalysis> Analyses { get; set; } = new List<TripAnalysis>();
        public virtual ICollection<SavedTrip> SavedTrips { get; set; } = new List<SavedTrip>();
    }
}
