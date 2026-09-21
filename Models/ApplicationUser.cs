using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace TripAnalyzer.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? MobileNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
        public virtual ICollection<SavedTrip> SavedTrips { get; set; } = new List<SavedTrip>();
        public virtual ICollection<Enquiry> Enquiries { get; set; } = new List<Enquiry>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
