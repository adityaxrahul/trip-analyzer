using System.Collections.Generic;
using System.Threading.Tasks;
using TripAnalyzer.Models;

namespace TripAnalyzer.Services
{
    public interface ITripRecommendationService
    {
        Task<List<TripAnalysis>> AnalyzeTripAsync(Trip trip, List<TransportOption> options);
    }
}
