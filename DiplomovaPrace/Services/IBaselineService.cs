using DiplomovaPrace.Models.Kpi;

namespace DiplomovaPrace.Services;

public interface IBaselineService
{
    Task<BaselineResult> CalculateBaselineSummaryAsync(string deviceId, DateTime from, DateTime to, int pastWeeksToAnalyze = 4, CancellationToken ct = default);
}
