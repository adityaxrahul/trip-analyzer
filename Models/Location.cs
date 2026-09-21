using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace TripAnalyzer.Models
{
    public class Location
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = string.Empty;

        [StringLength(10)]
        public string? CountryCode { get; set; }

        [StringLength(100)]
        public string? StateOrProvince { get; set; }

        [StringLength(10)]
        public string? StateCode { get; set; }

        [StringLength(100)]
        public string? DistrictOrCounty { get; set; }

        [StringLength(10)]
        public string? DistrictCode { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [Required]
        [StringLength(50)]
        public string LocationType { get; set; } = "City"; // Country, State, District, City, Locality

        public int? ParentLocationId { get; set; }
        
        [ForeignKey("ParentLocationId")]
        [JsonIgnore]
        public Location? ParentLocation { get; set; }
        
        [JsonIgnore]
        public ICollection<Location> ChildLocations { get; set; } = new List<Location>();

        public bool IsActive { get; set; } = true;
    }
}
