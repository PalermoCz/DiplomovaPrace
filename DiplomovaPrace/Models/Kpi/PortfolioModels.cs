namespace DiplomovaPrace.Models.Kpi;

public enum PortfolioAlertLevel
{
    Info,
    Warning,
    Critical
}

public record PortfolioInsight(
    string Message,
    PortfolioAlertLevel Level
);

public record BuildingBenchmarkResult(
    string BuildingId,
    string BuildingName,
    string? PrimaryUse,
    double? GrossFloorAreaM2,
    BaselineResult Baseline
)
{
    public double? SpecificConsumption => Baseline.CurrentPeriodKpi?.SpecificConsumptionKWhPerM2;
    public double? ActualConsumption => Baseline.ActualConsumptionKWh;
    public double? DeviationPercent => Baseline.DeviationPercent;
    public double? OffHoursConsumption => Baseline.CurrentPeriodKpi?.OffHoursConsumptionKWh;

    public List<PortfolioInsight> Insights { get; } = new();
}

public record PortfolioBenchmarkResult(
    DateTime From,
    DateTime To,
    IReadOnlyList<BuildingBenchmarkResult> Buildings
);
