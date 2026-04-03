namespace DiplomovaPrace.Models.Kpi;

public record KpiQuery(
    string DeviceId,
    DateTime From,
    DateTime To
);

public record MeterKpiResult(
    string DeviceId,
    int RecordCount,
    DateTime? FirstTimestamp,
    DateTime? LastTimestamp,
    TimeSpan? Duration,
    double? PeakPowerKW,
    double? AveragePowerKW,
    double? AverageVoltageV,
    double? AverageCurrentA,
    double? AveragePowerFactor,
    double? TotalConsumptionKWh,
    double? WorkingHoursConsumptionKWh,
    double? OffHoursConsumptionKWh,
    double? SpecificConsumptionKWhPerM2,
    bool IsEstimatedConsumption,
    string? ErrorMessage = null
)
{
    public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
}

public record KpiComparisonResult(
    string DeviceId,
    MeterKpiResult PeriodA,
    MeterKpiResult PeriodB,
    double? DiffConsumptionKWh,
    double? DiffPeakPowerKW,
    double? DiffAveragePowerKW
);
