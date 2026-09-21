using System.Threading.Tasks;

namespace TripAnalyzer.Services
{
    public class ModeEstimateResult
    {
        public string Mode { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public int DurationMinutes { get; set; }
        public string DurationText { get; set; } = string.Empty;
        public decimal BasePricePerPassenger { get; set; }
        public string CalculationSource { get; set; } = string.Empty;
        public string DistanceLabel { get; set; } = string.Empty;
        public string DurationLabel { get; set; } = string.Empty;
        public bool IsBenchmark { get; set; } = true;
    }

    public interface IFareCalculationService
    {
        Task<decimal> CalculateFareAsync(string mode, double distanceKm, int passengers = 1);
        Task<int> CalculateDurationMinutesAsync(string mode, double distanceKm);

        Task<ModeEstimateResult> EstimateTrainAsync(double roadDistanceKm, double straightLineKm, int passengers = 1);
        Task<ModeEstimateResult> EstimateFlightAsync(double straightLineKm);
        Task<ModeEstimateResult> EstimateBusAsync(double roadDistanceKm, int? googleRoadDurationMinutes, double straightLineKm);
        Task<ModeEstimateResult> EstimateCarAsync(double roadDistanceKm, int? googleRoadDurationMinutes, double straightLineKm);
    }
}
