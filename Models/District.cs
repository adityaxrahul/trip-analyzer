using System.ComponentModel.DataAnnotations;

namespace TripAnalyzer.Models
{
    public class District
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string StateCode { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string DistrictCode { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
