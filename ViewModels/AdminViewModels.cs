using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TripAnalyzer.Models;
using TripAnalyzer.Services;

namespace TripAnalyzer.ViewModels
{
    public class AdminDashboardViewModel
    {
        public AdminAnalyticsData Analytics { get; set; } = new AdminAnalyticsData();
        public List<Trip> RecentTrips { get; set; } = new List<Trip>();
        public List<Enquiry> RecentEnquiries { get; set; } = new List<Enquiry>();
        public List<ApplicationUser> RecentUsers { get; set; } = new List<ApplicationUser>();
    }

    public class TransportOptionViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Source { get; set; } = string.Empty;

        [Required]
        public string Destination { get; set; } = string.Empty;

        [Required]
        public string Mode { get; set; } = "Train"; // Train, Bus, Flight, Car

        [Required]
        [Display(Name = "Carrier / Service Name")]
        public string CarrierName { get; set; } = string.Empty;

        [Required]
        [Range(1, 100000, ErrorMessage = "Base price must be greater than 0.")]
        [Display(Name = "Base Price (₹)")]
        public decimal BasePricePerPassenger { get; set; }

        [Required]
        [Range(1, 10000, ErrorMessage = "Duration must be greater than 0 minutes.")]
        [Display(Name = "Duration (Minutes)")]
        public int DurationMinutes { get; set; }

        [Required]
        [Range(1, 100000, ErrorMessage = "Distance must be greater than 0 km.")]
        [Display(Name = "Distance (Km)")]
        public double DistanceKm { get; set; }

        [Range(0, 100)]
        [Display(Name = "Availability Rating (0-100)")]
        public double AvailabilityScore { get; set; } = 80.0;

        [Range(0, 100)]
        [Display(Name = "Comfort Rating (0-100)")]
        public double ComfortScore { get; set; } = 75.0;

        [Range(1, 5)]
        [Display(Name = "Eco Rating (1-5)")]
        public double EcoRating { get; set; } = 4.0;

        [Display(Name = "Departure Time")]
        public string DepartureTime { get; set; } = "08:00 AM";

        [Display(Name = "Arrival Time")]
        public string ArrivalTime { get; set; } = "08:00 PM";

        [Display(Name = "Amenities")]
        public string Amenities { get; set; } = "AC, Charging Outlets, Reclining Seats";

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Sample/Estimated Data")]
        public bool IsEstimatedData { get; set; } = true;
    }

    public class EnquiryManageViewModel
    {
        public Enquiry Enquiry { get; set; } = new Enquiry();

        [Required(ErrorMessage = "Response message is required to reply.")]
        [Display(Name = "Admin Reply")]
        public string AdminReply { get; set; } = string.Empty;

        [Display(Name = "Update Status")]
        public string Status { get; set; } = "Resolved";
    }

    public class ChatbotFaqManageViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Question")]
        public string Question { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Answer")]
        public string Answer { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Category")]
        public string Category { get; set; } = "General";

        [Display(Name = "Search Keywords (comma separated)")]
        public string Keywords { get; set; } = string.Empty;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;
    }

    public class UserManageViewModel
    {
        public ApplicationUser User { get; set; } = new ApplicationUser();
        public IList<string> Roles { get; set; } = new List<string>();
        public int TripCount { get; set; }
        public int SavedCount { get; set; }
        public int EnquiryCount { get; set; }
    }
}
