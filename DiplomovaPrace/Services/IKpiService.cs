namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models.Kpi;

public interface IKpiService
{
    Task<MeterKpiResult> CalculateBasicKpiAsync(KpiQuery query, CancellationToken ct = default);
    
    Task<KpiComparisonResult> ComparePeriodsAsync(
        string deviceId, 
        DateTime fromA, DateTime toA, 
        DateTime fromB, DateTime toB, 
        CancellationToken ct = default);
}
