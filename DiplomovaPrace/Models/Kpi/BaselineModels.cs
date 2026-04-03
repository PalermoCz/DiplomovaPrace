namespace DiplomovaPrace.Models.Kpi;

public enum BaselineStatus
{
    InsufficientData,
    Normal,
    BelowBaseline,
    SignificantlyBelowBaseline,
    AboveBaseline,
    SignificantlyAboveBaseline
}

public record BaselineResult(
    string DeviceId,
    DateTime From,
    DateTime To,
    double? ActualConsumptionKWh,
    double? ExpectedConsumptionKWh,
    double? DeviationKWh,
    double? DeviationPercent,
    BaselineStatus Status,
    int ValidHistoricalPeriodsUsed,
    MeterKpiResult? CurrentPeriodKpi,
    string? ErrorMessage = null
)
{
    public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
}
