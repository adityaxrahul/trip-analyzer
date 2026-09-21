using System.Collections.Generic;
using System.Threading.Tasks;

namespace TripAnalyzer.Services
{
    public class DashboardStats
    {
        public int TotalUsers { get; set; }
        public int TotalTrips { get; set; }
        public int PendingEnquiries { get; set; }
        public int TotalSavedTrips { get; set; }
        public string TopDestination { get; set; } = "N/A";
        public string PreferredTransport { get; set; } = "N/A";
    }

    public class ChartDataset
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<int> Data { get; set; } = new List<int>();
    }

    public class AdminAnalyticsData
    {
        public DashboardStats Stats { get; set; } = new DashboardStats();
        public ChartDataset TripsOverTime { get; set; } = new ChartDataset();
        public ChartDataset TransportDistribution { get; set; } = new ChartDataset();
        public ChartDataset PopularDestinations { get; set; } = new ChartDataset();
        public ChartDataset UserGrowth { get; set; } = new ChartDataset();
        public ChartDataset EnquiryStatusBreakdown { get; set; } = new ChartDataset();
    }

    public interface IAdminAnalyticsService
    {
        Task<AdminAnalyticsData> GetAnalyticsDataAsync();
    }
}
