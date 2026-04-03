using DiplomovaPrace.Models.Kpi;

namespace DiplomovaPrace.Services;

public class BaselineService : IBaselineService
{
    private readonly IKpiService _kpiService;

    public BaselineService(IKpiService kpiService)
    {
        _kpiService = kpiService;
    }

    public async Task<BaselineResult> CalculateBaselineSummaryAsync(
        string deviceId, 
        DateTime from, 
        DateTime to, 
        int pastWeeksToAnalyze = 4, 
        CancellationToken ct = default)
    {
        // 1. Spočítat aktuální spotřebu
        var currentKpi = await _kpiService.CalculateBasicKpiAsync(new KpiQuery(deviceId, from, to), ct);
        
        if (!currentKpi.IsSuccess || ((currentKpi.TotalConsumptionKWh ?? 0) == 0 && currentKpi.RecordCount == 0))
        {
            return new BaselineResult(deviceId, from, to, null, null, null, null, BaselineStatus.InsufficientData, 0, currentKpi, currentKpi.ErrorMessage ?? "Žádná vstupní data.");
        }

        double actualConsumption = currentKpi.TotalConsumptionKWh ?? 0;

        // 2. Projít N předchozích týdnů a stáhnout spotřeby
        var pastConsumptions = new List<double>();
        
        for (int i = 1; i <= pastWeeksToAnalyze; i++)
        {
            var offset = TimeSpan.FromDays(7 * i);
            var pastFrom = from.Subtract(offset);
            var pastTo = to.Subtract(offset);

            var pastKpi = await _kpiService.CalculateBasicKpiAsync(new KpiQuery(deviceId, pastFrom, pastTo), ct);
            if (pastKpi.IsSuccess && pastKpi.RecordCount > 0 && pastKpi.TotalConsumptionKWh.HasValue)
            {
                pastConsumptions.Add(pastKpi.TotalConsumptionKWh.Value);
            }
        }

        if (pastConsumptions.Count == 0)
        {
            return new BaselineResult(deviceId, from, to, actualConsumption, null, null, null, BaselineStatus.InsufficientData, 0, currentKpi);
        }

        double expectedConsumption = pastConsumptions.Average();
        
        // --- GUARDRAILS ---
        // Pravidlo 1: Potřebujeme alespoň N platných referenčních oken (např. 2), abychom měli trochu jistotu.
        if (pastConsumptions.Count < 2)
        {
            return new BaselineResult(deviceId, from, to, actualConsumption, expectedConsumption, null, null, BaselineStatus.InsufficientData, pastConsumptions.Count, currentKpi, "Nedostatek historických dat (méně než 2 referenční týdny).");
        }

        // Pravidlo 2: Pokud je referenční spotřeba prakticky nula nebo velmi nízká (např. v průměru < 10 kWh na den), 
        // tak vyletí procentuální odchylka do stovek a tisíců procent (šum). Není to stabilní základna pro procentuální srovnání velkých budov.
        double daysInInterval = (to - from).TotalDays;
        if (daysInInterval <= 0) daysInInterval = 1;
        
        double expectedPerDay = expectedConsumption / daysInInterval;
        if (expectedPerDay < 10.0)
        {
            return new BaselineResult(deviceId, from, to, actualConsumption, expectedConsumption, null, null, BaselineStatus.InsufficientData, pastConsumptions.Count, currentKpi, "Nízká referenční hodnota pro výpočet spolehlivých procent (pod 10 kWh denně).");
        }

        // 4. Výpočet odchylky
        double deviationKWh = actualConsumption - expectedConsumption;
        double? deviationPercent = (deviationKWh / expectedConsumption) * 100.0;

        // 5. Jednoduchá rulesetová klasifikace stavu (thresholdy: 10% a 20%)
        var status = BaselineStatus.Normal;
        
        if (deviationPercent.HasValue)
        {
            if (deviationPercent.Value > 20)
                status = BaselineStatus.SignificantlyAboveBaseline;
            else if (deviationPercent.Value > 10)
                status = BaselineStatus.AboveBaseline;
            else if (deviationPercent.Value < -20)
                status = BaselineStatus.SignificantlyBelowBaseline;
            else if (deviationPercent.Value < -10)
                status = BaselineStatus.BelowBaseline;
        }

        return new BaselineResult(
            DeviceId: deviceId,
            From: from,
            To: to,
            ActualConsumptionKWh: actualConsumption,
            ExpectedConsumptionKWh: expectedConsumption,
            DeviationKWh: deviationKWh,
            DeviationPercent: deviationPercent,
            Status: status,
            ValidHistoricalPeriodsUsed: pastConsumptions.Count,
            CurrentPeriodKpi: currentKpi
        );
    }
}
