using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TripAnalyzer.Data;
using TripAnalyzer.Models;

namespace TripAnalyzer.Services
{
    public class DistanceCalculationService : IDistanceCalculationService
    {
        private readonly ApplicationDbContext _db;

        public DistanceCalculationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task<double> CalculateDistanceAsync(District source, District destination)
        {
            if (source == null || destination == null) return Task.FromResult(0.0);

            // Using Haversine formula for straight-line distance
            double R = 6371; // Earth radius in kilometers
            double dLat = ToRadians(destination.Latitude - source.Latitude);
            double dLon = ToRadians(destination.Longitude - source.Longitude);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(source.Latitude)) * Math.Cos(ToRadians(destination.Latitude)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = R * c;

            // Return exact straight-line distance
            return Task.FromResult(distance);
        }

        public async Task<double> CalculateDistanceByNameAsync(string sourceName, string destinationName)
        {
            var sourceCoords = await ResolveCoordinatesAsync(sourceName);
            var destCoords = await ResolveCoordinatesAsync(destinationName);

            if (sourceCoords != null && destCoords != null)
            {
                double R = 6371; // Earth radius in kilometers
                double dLat = ToRadians(destCoords.Value.Latitude - sourceCoords.Value.Latitude);
                double dLon = ToRadians(destCoords.Value.Longitude - sourceCoords.Value.Longitude);

                double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                           Math.Cos(ToRadians(sourceCoords.Value.Latitude)) * Math.Cos(ToRadians(destCoords.Value.Latitude)) *
                           Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

                double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                return R * c;
            }

            return 0.0;
        }

        private async Task<(double Latitude, double Longitude)?> ResolveCoordinatesAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var nameTrimmed = name.Trim();

            // 1. Try City match
            var city = await _db.Cities.FirstOrDefaultAsync(c => c.IsActive && EF.Functions.ILike(c.Name, nameTrimmed));
            if (city != null)
            {
                return (city.Latitude, city.Longitude);
            }

            // 2. Try District match
            var district = await _db.Districts.FirstOrDefaultAsync(d => d.IsActive && EF.Functions.ILike(d.Name, nameTrimmed));
            if (district != null)
            {
                return (district.Latitude, district.Longitude);
            }

            return null;
        }

        private static double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }
    }
}
