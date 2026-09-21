using System;

namespace TripAnalyzer.Models
{
    public class SavedTrip
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        public int TripId { get; set; }
        public virtual Trip? Trip { get; set; }

        public string? Notes { get; set; }
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
