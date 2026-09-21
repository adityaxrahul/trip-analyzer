using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TripAnalyzer.Models
{
    public class City
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
        public int DistrictId { get; set; }

        [ForeignKey("DistrictId")]
        public District? District { get; set; }

        [Required]
        [StringLength(20)]
        public string CityCode { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
