using System;
using System.ComponentModel.DataAnnotations;

namespace TripAnalyzer.Models
{
    public class FareConfiguration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Mode { get; set; } = string.Empty; // Train, Bus, Flight, Car

        public decimal BaseFare { get; set; }
        public decimal PerKmRate { get; set; }
        public decimal MinimumFare { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
