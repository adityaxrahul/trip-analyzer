using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TripAnalyzer.Data;

namespace TripAnalyzer.Services
{
    public class AdminAnalyticsService : IAdminAnalyticsService
    {
        private readonly ApplicationDbContext _db;

        public AdminAnalyticsService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<AdminAnalyticsData> GetAnalyticsDataAsync()
        {
            var data = new AdminAnalyticsData();

            // Stats
            data.Stats.TotalUsers = await _db.Users.CountAsync();
            data.Stats.TotalTrips = await _db.Trips.CountAsync();
            data.Stats.PendingEnquiries = await _db.Enquiries.CountAsync(e => e.Status == "Pending");
            data.Stats.TotalSavedTrips = await _db.SavedTrips.CountAsync();

            var topDestGroup = await _db.Trips
                .GroupBy(t => t.Destination)
                .Select(g => new { Destination = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            data.Stats.TopDestination = topDestGroup?.Destination ?? "None";

            var topModeGroup = await _db.TripAnalyses
                .Include(ta => ta.TransportOption)
                .Where(ta => ta.IsRecommended && ta.TransportOption != null)
                .GroupBy(ta => ta.TransportOption!.Mode)
                .Select(g => new { Mode = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            data.Stats.PreferredTransport = topModeGroup?.Mode ?? "Train";

            // Trips over last 7 days
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-6 + i))
                .ToList();

            var tripsByDate = await _db.Trips
                .Where(t => t.CreatedAt >= DateTime.UtcNow.Date.AddDays(-7))
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            data.TripsOverTime.Labels = last7Days.Select(d => d.ToString("MMM dd")).ToList();
            data.TripsOverTime.Data = last7Days
                .Select(d => tripsByDate.FirstOrDefault(x => x.Date == d)?.Count ?? 0)
                .ToList();

            // Transport Preference Distribution
            var modeDistribution = await _db.TripAnalyses
                .Include(ta => ta.TransportOption)
                .Where(ta => ta.TransportOption != null)
                .GroupBy(ta => ta.TransportOption!.Mode)
                .Select(g => new { Mode = g.Key, Count = g.Count() })
                .ToListAsync();

            data.TransportDistribution.Labels = modeDistribution.Select(m => m.Mode).ToList();
            data.TransportDistribution.Data = modeDistribution.Select(m => m.Count).ToList();
            if (!data.TransportDistribution.Labels.Any())
            {
                data.TransportDistribution.Labels = new List<string> { "Train", "Bus", "Flight", "Car" };
                data.TransportDistribution.Data = new List<int> { 45, 25, 20, 10 };
            }

            // Popular Destinations (Top 5)
            var topDests = await _db.Trips
                .GroupBy(t => t.Destination)
                .Select(g => new { Destination = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            data.PopularDestinations.Labels = topDests.Select(d => d.Destination).ToList();
            data.PopularDestinations.Data = topDests.Select(d => d.Count).ToList();
            if (!data.PopularDestinations.Labels.Any())
            {
                data.PopularDestinations.Labels = new List<string> { "Mumbai", "Ahmedabad", "Delhi", "Rajkot", "Goa" };
                data.PopularDestinations.Data = new List<int> { 35, 28, 22, 15, 10 };
            }

            // User Growth over 7 days
            var usersByDate = await _db.Users
                .Where(u => u.CreatedAt >= DateTime.UtcNow.Date.AddDays(-7))
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            data.UserGrowth.Labels = last7Days.Select(d => d.ToString("MMM dd")).ToList();
            data.UserGrowth.Data = last7Days
                .Select(d => usersByDate.FirstOrDefault(x => x.Date == d)?.Count ?? 0)
                .ToList();

            // Enquiry Status Breakdown
            var enquiryStatus = await _db.Enquiries
                .GroupBy(e => e.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            data.EnquiryStatusBreakdown.Labels = enquiryStatus.Select(e => e.Status).ToList();
            data.EnquiryStatusBreakdown.Data = enquiryStatus.Select(e => e.Count).ToList();
            if (!data.EnquiryStatusBreakdown.Labels.Any())
            {
                data.EnquiryStatusBreakdown.Labels = new List<string> { "Pending", "In Progress", "Resolved" };
                data.EnquiryStatusBreakdown.Data = new List<int> { 5, 2, 8 };
            }

            return data;
        }
    }
}
