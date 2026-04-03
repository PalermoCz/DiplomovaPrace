using DiplomovaPrace.Models;
using DiplomovaPrace.Models.Configuration;
using DiplomovaPrace.Models.Kpi;

namespace DiplomovaPrace.Services;

public class PortfolioAnalyticsService : IPortfolioAnalyticsService
{
    private readonly IBuildingConfigurationService _configService;
    private readonly IBaselineService _baselineService;
    private readonly IActiveBuildingService _activeBuildingService;

    public PortfolioAnalyticsService(
        IBuildingConfigurationService configService, 
        IBaselineService baselineService,
        IActiveBuildingService activeBuildingService)
    {
        _configService = configService;
        _baselineService = baselineService;
        _activeBuildingService = activeBuildingService;
    }

    public async Task<PortfolioBenchmarkResult> GetPortfolioBenchmarkAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var buildings = await _configService.GetAllBuildingsAsync();
        var benchmarkResults = new List<BuildingBenchmarkResult>();

        // Uschováme původní ActiveBuilding, abychom ho nemodifikovali trvale
        var originalActiveBuilding = _activeBuildingService.ActiveBuildingId;

        try
        {
            foreach (var building in buildings)
            {
                // ActiveBuildingService must switch context so that KpiService internally gets the right area & hours
                _activeBuildingService.SetActiveBuilding(building.Id);

                // Najít hlavní měřidlo budovy pro účely portfolia (historicky např. em1 u BDG2 datasetu)
                var mainDevice = FindPrimaryMeter(building);
                if (mainDevice == null) continue; // Nevyhodnocujeme budovy bez měření

                var baseline = await _baselineService.CalculateBaselineSummaryAsync(mainDevice.Id, from, to, 4, ct);
                
                var result = new BuildingBenchmarkResult(
                    BuildingId: building.Id,
                    BuildingName: building.Name,
                    PrimaryUse: building.Metadata?.SiteName, // Mapováno na SiteName pro kontext
                    GrossFloorAreaM2: building.Metadata?.GrossFloorAreaM2,
                    Baseline: baseline
                );

                EvaluateInsights(result);
                benchmarkResults.Add(result);
            }
        }
        finally
        {
            // Opatrně vrátíme zpět původní kontext
            if (originalActiveBuilding != null)
                _activeBuildingService.SetActiveBuilding(originalActiveBuilding);
        }

        return new PortfolioBenchmarkResult(from, to, benchmarkResults);
    }

    private DeviceConfig? FindPrimaryMeter(BuildingConfig building)
    {
        // 1. Zkusit najít hlavní elektroměr (em1 z BDG2 logiky)
        foreach (var floor in building.Floors)
        {
            foreach (var room in floor.Rooms)
            {
                var device = room.Devices.FirstOrDefault(d => d.Id.EndsWith(":em1", StringComparison.OrdinalIgnoreCase));
                if (device != null) return device;
            }
        }

        // 2. Fallback na první nalezené Smart Metering zařízení
        foreach (var floor in building.Floors)
        {
            foreach (var room in floor.Rooms)
            {
                var device = room.Devices.FirstOrDefault(d => d.Type.IsMeteringDevice());
                if (device != null) return device;
            }
        }

        return null; // Žádné měřicí zařízení nenalezeno
    }

    private void EvaluateInsights(BuildingBenchmarkResult result)
    {
        if (!result.Baseline.IsSuccess || result.Baseline.Status == BaselineStatus.InsufficientData)
            return;

        // Pravidlo 1: Extrémní překročení baseline (pouze pokud je absolutní nárůst také významný, např > 50 kWh)
        var deviationKwhAbs = Math.Abs(result.Baseline.DeviationKWh ?? 0);
        if (result.DeviationPercent > 20 && result.Baseline.DeviationKWh > 50)
        {
            result.Insights.Add(new PortfolioInsight(
                $"Spotřeba narostla o {result.DeviationPercent.Value:0.#} % (relativně o +{result.Baseline.DeviationKWh.Value:0} kWh) nad historickou baseline.",
                PortfolioAlertLevel.Critical));
        }
        else if (result.DeviationPercent > 10 && result.Baseline.DeviationKWh > 20)
        {
            result.Insights.Add(new PortfolioInsight(
                $"Zvýšená spotřeba ({result.DeviationPercent.Value:0.#} % nad normálem).",
                PortfolioAlertLevel.Warning));
        }

        // Pravidlo 2: Velká spotřeba mimo pracovní dobu
        if (result.ActualConsumption > 0 && result.OffHoursConsumption.HasValue)
        {
            double offHoursRatio = result.OffHoursConsumption.Value / result.ActualConsumption.Value;
            if (offHoursRatio > 0.40)
            {
                result.Insights.Add(new PortfolioInsight(
                    $"{offHoursRatio * 100:0.#} % energie bylo spotřebováno mimo pracovní dobu.",
                    PortfolioAlertLevel.Warning));
            }
        }

        // Pravidlo 3: Vysoká specifická spotřeba (m2) - prahová hodnota např > 1.5 kWh/m2 / obvyklý interval
        // Toto by ideálně záviselo na délce sledovaného období (např. 0.1 kWh/m2 na den).
        // Zatím jednoduchý threshold pro demonstraci logiky. Zvažujeme interval např 7 dní.
        var durationDays = (result.Baseline.To - result.Baseline.From).TotalDays;
        if (durationDays > 0 && result.SpecificConsumption.HasValue)
        {
            double dailySpecific = result.SpecificConsumption.Value / durationDays;
            if (dailySpecific > 0.5) // 0.5 kWh na m2 za DEN je příliš. Pro představu budova otepelněná má pod 0.3
            {
                result.Insights.Add(new PortfolioInsight(
                    $"Velmi vysoká spotřeba na m² ({dailySpecific:0.##} kWh/m²/den).",
                    PortfolioAlertLevel.Critical));
            }
        }
    }
}
