using System.Threading.Tasks;
using TripAnalyzer.Models;

namespace TripAnalyzer.Services
{
    public interface IDistanceCalculationService
    {
        Task<double> CalculateDistanceAsync(District source, District destination);
        Task<double> CalculateDistanceByNameAsync(string sourceName, string destinationName);
    }
}
