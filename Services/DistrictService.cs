using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TripAnalyzer.Data;
using TripAnalyzer.Models;

namespace TripAnalyzer.Services
{
    public class DistrictService : IDistrictService
    {
        private readonly ApplicationDbContext _db;

        public DistrictService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<District>> SearchDistrictsAsync(string searchTerm, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<District>();

            return await _db.Districts
                .Where(d => d.IsActive && d.Name.ToLower().Contains(searchTerm.ToLower()))
                .OrderBy(d => d.Name)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<District?> GetDistrictByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return await _db.Districts
                .FirstOrDefaultAsync(d => d.Name.ToLower() == name.ToLower() && d.IsActive);
        }
    }
}
