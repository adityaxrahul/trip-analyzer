using System.Collections.Generic;
using System.Threading.Tasks;
using TripAnalyzer.Models;

namespace TripAnalyzer.Services
{
    public interface IDistrictService
    {
        Task<List<District>> SearchDistrictsAsync(string searchTerm, int limit = 10);
        Task<District?> GetDistrictByNameAsync(string name);
    }
}
