using DiplomovaPrace.Models.Kpi;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;

namespace DiplomovaPrace.Services;

public class CuratedNodeSummary
{
    public string Title { get; set; } = string.Empty;
    public double TotalSum { get; set; }
    public double Average { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public int DataPoints { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string SummaryLabel { get; set; } = string.Empty;
    public string StatsUnit { get; set; } = string.Empty;
    public string StatsLabel { get; set; } = string.Empty;
}

public sealed class CuratedNodeTimeSeriesPoint
{
    public DateTime TimestampUtc { get; init; }
    public double Value { get; init; }
}

public sealed class CuratedNodeTimeSeriesResult
{
    public string NodeKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string YAxisLabel { get; init; } = string.Empty;
    public FacilitySignalSeriesSemantics SeriesSemantics { get; init; } = FacilitySignalSeriesSemantics.SampleSeries;
    public bool UsesDerivedIntervalSeries { get; init; }
    public string? SeriesStatusMessage { get; init; }
    public CuratedNodeTimeSeriesGranularity Granularity { get; init; } = CuratedNodeTimeSeriesGranularity.Raw15Min;
    public string GranularityLabel { get; init; } = "15min detail";
    public string AggregationMethod { get; init; } = "No aggregation (raw series).";
    public string InterpretationNote { get; init; } = string.Empty;
    public CuratedNodeTimeSeriesMode RequestedMode { get; init; } = CuratedNodeTimeSeriesMode.Auto;
    public string RequestedModeLabel { get; init; } = "Auto";
    public bool BaselineOverlayRequested { get; init; }
    public bool BaselineOverlayAvailable { get; init; }
    public string? BaselineOverlayMessage { get; init; }
    public IReadOnlyList<CuratedNodeTimeSeriesPoint> BaselinePoints { get; init; } = [];
    public string? NoDataMessage { get; init; }
    public IReadOnlyList<CuratedNodeTimeSeriesPoint> Points { get; init; } = [];
}

public sealed class CuratedNodeCompareSeries
{
    public string NodeKey { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public string? NoDataMessage { get; init; }
    public IReadOnlyList<CuratedNodeTimeSeriesPoint> Points { get; init; } = [];
}

public sealed class CuratedNodeCompareTimeSeriesResult
{
    public string PrimaryNodeKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string YAxisLabel { get; init; } = string.Empty;
    public CuratedNodeTimeSeriesGranularity Granularity { get; init; } = CuratedNodeTimeSeriesGranularity.Raw15Min;
    public string GranularityLabel { get; init; } = "15min detail";
    public string AggregationMethod { get; init; } = "No aggregation (raw series).";
    public string InterpretationNote { get; init; } = string.Empty;
    public CuratedNodeTimeSeriesMode RequestedMode { get; init; } = CuratedNodeTimeSeriesMode.Auto;
    public string RequestedModeLabel { get; init; } = "Auto";
    public IReadOnlyList<CuratedNodeCompareSeries> Series { get; init; } = [];
    public IReadOnlyList<string> ExcludedNodeMessages { get; init; } = [];
    public string? NoDataMessage { get; init; }
}

public sealed class CuratedSelectionAggregateOverviewResult
{
    public CuratedNodeSummary? Summary { get; init; }
    public CuratedNodeTimeSeriesResult? TimeSeries { get; init; }
    public CuratedSelectionLoadProfileSummary LoadProfile { get; init; } = new();
    public CuratedSelectionLoadDurationCurveSummary LoadDurationCurve { get; init; } = new();
    public CuratedSelectionPeakAnalysisSummary PeakAnalysis { get; init; } = new();
    public CuratedSelectionLoadFactorSummary LoadFactor { get; init; } = new();
    public CuratedSelectionAfterHoursLoadSummary AfterHoursLoad { get; init; } = new();
    public CuratedSelectionOperatingRegimeSummary OperatingRegime { get; init; } = new();
    public CuratedSelectionEmsEvaluationSummary EmsEvaluation { get; init; } = new();
    public IReadOnlyList<CuratedSelectionContributionItem> Breakdown { get; init; } = [];
    public IReadOnlyList<CuratedSelectionRoleContributionItem> RoleBreakdown { get; init; } = [];
    public CuratedSelectionDisaggregationSummary Disaggregation { get; init; } = new();
    public CuratedSelectionContributionIntelligenceSummary ContributionIntelligence { get; init; } = new();
    public CuratedSelectionSourceMapSummary SourceMap { get; init; } = new();
    public CuratedSelectionCoverageSummary Coverage { get; init; } = new();
    public CuratedSelectionOperationalHealthSummary OperationalHealth { get; init; } = new();
    public CuratedNodeCompareTimeSeriesResult? ForecastCompareTimeSeries { get; init; }
    public CuratedSelectionForecastDiagnosticsSummary ForecastDiagnostics { get; init; } = new();
    public CuratedAggregateEnergyProfile EnergyProfile { get; init; } = CuratedAggregateEnergyProfile.Neutral;
    public bool HasNegativeContributions { get; init; }
    public double TotalConsumptionKwh { get; init; }
    public double TotalGenerationKwh { get; init; }
    public double NetEnergyKwh { get; init; }
    public double HeadlineValueKwh { get; init; }
    public string HeadlineLabel { get; init; } = "Net energy balance";
    public string HeadlineDescription { get; init; } = string.Empty;
    public bool IsNetHeadline { get; init; }
    public IReadOnlyList<string> SupportedNodeKeys { get; init; } = [];
    public IReadOnlyList<string> UnsupportedNodeKeys { get; init; } = [];
    public IReadOnlyList<string> ContextOnlyNodeKeys { get; init; } = [];
    public IReadOnlyList<string> NoDataNodeKeys { get; init; } = [];
    public IReadOnlyList<string> IncludedNodeKeys { get; init; } = [];
    public string? Message { get; init; }
}

public sealed class CuratedSelectionAggregateRequestOptions
{
    public bool IncludeBreakdown { get; init; } = true;
    public bool IncludePerformance { get; init; } = true;
    public bool IncludeDiagnostics { get; init; } = true;

    public static CuratedSelectionAggregateRequestOptions OverviewOnly { get; } = new()
    {
        IncludeBreakdown = false,
        IncludePerformance = false,
        IncludeDiagnostics = false
    };
}

public enum SelectionSignalAvailabilityKind
{
    SingleNodeSeries,
    AggregateSeries,
    AggregateUnavailable
}

public sealed class SelectionSignalAvailabilityItem
{
    public FacilitySignalCode ExactSignalCode { get; init; }
    public FacilitySignalFamily SignalFamily { get; init; } = FacilitySignalFamily.Custom;
    public string Unit { get; init; } = string.Empty;
    public int ScopeNodeCount { get; init; }
    public int MatchingNodeCount { get; init; }
    public int NonMatchingNodeCount { get; init; }
    public SelectionSignalAvailabilityKind AvailabilityKind { get; init; } = SelectionSignalAvailabilityKind.SingleNodeSeries;
    public string AvailabilityLabel { get; init; } = string.Empty;
    public string AvailabilityMessage { get; init; } = string.Empty;
    public IReadOnlyList<string> MatchingNodeKeys { get; init; } = [];

    public bool CanRenderSingleNodeSeries => AvailabilityKind == SelectionSignalAvailabilityKind.SingleNodeSeries;
    public bool CanAggregate => AvailabilityKind == SelectionSignalAvailabilityKind.AggregateSeries;
}

public sealed class SelectionSignalAvailabilityResult
{
    public IReadOnlyList<SelectionSignalAvailabilityItem> Options { get; init; } = [];
    public string? Message { get; init; }
}

public sealed class SelectionSignalBasicStats
{
    public double Min { get; init; }
    public double Max { get; init; }
    public double Average { get; init; }
    public int PointCount { get; init; }
    public DateTime? FirstTimestampUtc { get; init; }
    public DateTime? LastTimestampUtc { get; init; }
    public string Unit { get; init; } = string.Empty;
}

public sealed class SelectionPowerMetricSummary
{
    public bool IsAvailable { get; init; }
    public CuratedPerformanceKpiState State { get; init; } = CuratedPerformanceKpiState.Unavailable;
    public double? Value { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string StateReason { get; init; } = string.Empty;
}

public sealed class SelectionPowerDurationSummary
{
    public bool IsAvailable { get; init; }
    public CuratedPerformanceKpiState State { get; init; } = CuratedPerformanceKpiState.Unavailable;
    public double? DurationHours { get; init; }
    public double? ShareRatio { get; init; }
    public double? ThresholdValue { get; init; }
    public string ThresholdUnit { get; init; } = string.Empty;
    public int MatchingBucketCount { get; init; }
    public int TotalBucketCount { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string StateReason { get; init; } = string.Empty;
}

public sealed class SelectionPowerBasePeakDaySummary
{
    public DateTime DayUtc { get; init; }
    public int SampleCount { get; init; }
    public double BaseValue { get; init; }
    public double PeakValue { get; init; }
}

public sealed class SelectionPowerBasePeakOverTimeResult
{
    public bool IsAvailable { get; init; }
    public CuratedPerformanceKpiState State { get; init; } = CuratedPerformanceKpiState.Unavailable;
    public int UsableDayCount { get; init; }
    public int TotalDayCount { get; init; }
    public string SignalCode { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string GranularityLabel { get; init; } = string.Empty;
    public string EvaluationBasis { get; init; } = string.Empty;
    public string Summary { get; init; } = "Base vs Peak Over Time waits for an active exact power signal.";
    public string StateReason { get; init; } = string.Empty;
    public string Methodology { get; init; } = "Base over time = daily 5th percentile of the active power series. Peak over time = daily 95th percentile of the same series. Each usable day requires sub-daily power samples; no fallback signal is used.";
    public IReadOnlyList<SelectionPowerBasePeakDaySummary> Days { get; init; } = [];
    public CuratedNodeCompareTimeSeriesResult Chart { get; init; } = new();
}

public sealed class SelectionPowerAnalyticsResult
{
    public bool IsPowerSignal { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsMixedSignAggregateUnavailable { get; init; }
    public int PointCount { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string EvaluationBasis { get; init; } = string.Empty;
    public string Summary { get; init; } = "Power Analytics wait for an active exact signal code.";
    public string Methodology { get; init; } = "Near-base = 5th percentile of the active power series. Near-peak = 95th percentile of the same series. Peak-base ratio = near-peak / near-base when near-base is numerically safe. On-hour duration counts buckets at or above the midpoint between near-base and near-peak. After-hours Load reuses that same threshold only for fixed after-hours buckets (weekday outside 07:00-19:00 UTC plus weekends). Load duration curve sorts the same active power series in descending order without falling back to another signal.";
    public string DistinctionNote { get; init; } = "On-hour duration shows how long the active power series stays in its higher-load mode across the whole interval. After-hours Load uses the same threshold, but only inside fixed after-hours windows, so it reads as after-hours persistence rather than total runtime.";
    public SelectionPowerMetricSummary NearBase { get; init; } = new();
    public SelectionPowerMetricSummary NearPeak { get; init; } = new();
    public SelectionPowerMetricSummary PeakBaseRatio { get; init; } = new();
    public SelectionPowerDurationSummary OnHourDuration { get; init; } = new();
    public SelectionPowerDurationSummary AfterHoursLoad { get; init; } = new();
    public SelectionPowerBasePeakOverTimeResult BasePeakOverTime { get; init; } = new();
    public CuratedSelectionLoadDurationCurveSummary LoadDurationCurve { get; init; } = new();
}

public sealed class SelectionWeatherAwareBaselineResult
{
    public bool IsApplicable { get; init; }
    public bool IsAvailable { get; init; }
    public NodeDeviationSeverity? Severity { get; init; }
    public double? ActualValue { get; init; }
    public double? BaselineExpectedValue { get; init; }
    public double? DeltaAbsolute { get; init; }
    public double? DeltaPercent { get; init; }
    public double? CvRmsePercent { get; init; }
    public double? NmbePercent { get; init; }
    public int FitDayCount { get; init; }
    public int PredictionDayCount { get; init; }
    public int ContributingNodeCount { get; init; }
    public string Unit { get; init; } = "kWh";
    public string EvaluationBasis { get; init; } = string.Empty;
    public string Summary { get; init; } = "Weather-aware baseline waits for an active energy or power signal.";
    public string Methodology { get; init; } = "Daily weather-aware baseline uses daily energy with facility outdoor-air temperature Ta and an HDD/CDD linear model. Diagnostics report CV(RMSE) and NMBE on the fit period.";
    public string? Message { get; init; }
    public string? WeatherNodeKey { get; init; }
}

public sealed class SelectionEuiResult
{
    public bool IsApplicable { get; init; }
    public bool IsAvailable { get; init; }
    public double? EnergyKwh { get; init; }
    public double? FloorAreaM2 { get; init; }
    public double? EuiKwhPerM2 { get; init; }
    public int ContributingNodeCount { get; init; }
    public string SignalCode { get; init; } = string.Empty;
    public string EnergyUnit { get; init; } = "kWh";
    public string FloorAreaUnit { get; init; } = "m²";
    public string EuiUnit { get; init; } = "kWh/m²";
    public string Summary { get; init; } = "EUI waits for an active exact energy or power signal and explicit floor area metadata.";
    public string Methodology { get; init; } = "EUI = Energy / Floor area. This MVP reports period EUI over the selected interval, not an annualized EUI. Floor area must be explicitly entered on an area node, and power-derived energy is integrated over actual timestamp spacing before normalization to kWh.";
    public string EvaluationBasis { get; init; } = string.Empty;
    public string StateReason { get; init; } = string.Empty;
    public string? Message { get; init; }
    public string? FloorAreaNodeKey { get; init; }
}

public sealed class SelectionTemperatureLoadScatterPoint
{
    public DateTime TimestampUtc { get; init; }
    public double OutdoorTemperatureC { get; init; }
    public double LoadValue { get; init; }
}

public sealed class SelectionTemperatureLoadScatterResult
{
    public bool IsApplicable { get; init; }
    public bool IsAvailable { get; init; }
    public bool UsesEnergyDerivedLoad { get; init; }
    public int PointCount { get; init; }
    public int ContributingNodeCount { get; init; }
    public string SignalCode { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string GranularityLabel { get; init; } = "Hourly pairing";
    public string XAxisLabel { get; init; } = "Outdoor temperature Ta (C)";
    public string YAxisLabel { get; init; } = "Load";
    public string LoadBasisLabel { get; init; } = string.Empty;
    public string EvaluationBasis { get; init; } = string.Empty;
    public string Summary { get; init; } = "Temperature vs load scatter waits for an active energy or power signal.";
    public string Methodology { get; init; } = "Temperature vs load scatter uses complete UTC hours only. X axis = facility outdoor-air temperature Ta. Y axis = hourly average power for power signals, or hourly energy-derived load for energy signals. No fallback signal is used.";
    public string? Message { get; init; }
    public string? WeatherNodeKey { get; init; }
    public IReadOnlyList<SelectionTemperatureLoadScatterPoint> Points { get; init; } = [];
}

public sealed class SelectionSignalAnalyticsResult
{
    public SelectionSignalAvailabilityItem? SelectionSignal { get; init; }
    public CuratedNodeTimeSeriesResult? TimeSeries { get; init; }
    public SelectionSignalBasicStats? BasicStats { get; init; }
    public SelectionPowerAnalyticsResult PowerAnalytics { get; init; } = new();
    public SelectionEuiResult Eui { get; init; } = new();
    public SelectionWeatherAwareBaselineResult WeatherAwareBaseline { get; init; } = new();
    public SelectionTemperatureLoadScatterResult TemperatureLoadScatter { get; init; } = new();
    public bool IsAvailable { get; init; }
    public bool IsAggregate { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> ContributingNodeKeys { get; init; } = [];
}

public sealed class CuratedSelectionContributionItem
{
    public string NodeKey { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public FacilityNodeRole NodeRole { get; init; } = FacilityNodeRole.OtherUnclassified;
    public string NodeRoleLabel { get; init; } = FacilityNodeSemantics.GetRoleLabel(FacilityNodeRole.OtherUnclassified);
    public bool HasData { get; init; }
    public double IntervalEnergyKwh { get; init; }
    public CuratedEnergyContributionRole ContributionRole { get; init; } = CuratedEnergyContributionRole.Neutral;
    public double? SharePercent { get; init; }
}

public sealed class CuratedSelectionRoleContributionItem
{
    public FacilityNodeRole Role { get; init; } = FacilityNodeRole.OtherUnclassified;
    public string RoleLabel { get; init; } = FacilityNodeSemantics.GetRoleLabel(FacilityNodeRole.OtherUnclassified);
    public int NodeCount { get; init; }
    public int ContributingNodeCount { get; init; }
    public bool HasData { get; init; }
    public bool HasMixedSigns { get; init; }
    public double IntervalEnergyKwh { get; init; }
    public CuratedEnergyContributionRole ContributionRole { get; init; } = CuratedEnergyContributionRole.Neutral;
    public double? SharePercent { get; init; }
}

public enum CuratedContributionDirection
{
    IncreasesLoad,
    ReducesNetBalance,
    Neutral
}

public sealed class CuratedSelectionDisaggregationSummary
{
    public string Methodology { get; init; } = string.Empty;
    public string CompositionSummary { get; init; } = string.Empty;
    public int MeasuredContributorCount { get; init; }
    public int ConsumptionContributorCount { get; init; }
    public int GenerationContributorCount { get; init; }
    public bool HasMixedSigns { get; init; }
    public double TotalAbsoluteContributionKwh { get; init; }
}

public sealed class CuratedSelectionTopContributorItem
{
    public string NodeKey { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public FacilityNodeRole NodeRole { get; init; } = FacilityNodeRole.OtherUnclassified;
    public string NodeRoleLabel { get; init; } = FacilityNodeSemantics.GetRoleLabel(FacilityNodeRole.OtherUnclassified);
    public double IntervalEnergyKwh { get; init; }
    public double AbsoluteSharePercent { get; init; }
    public CuratedEnergyContributionRole ContributionRole { get; init; } = CuratedEnergyContributionRole.Neutral;
    public CuratedContributionDirection Direction { get; init; } = CuratedContributionDirection.Neutral;
}

public sealed class CuratedSelectionContributionIntelligenceSummary
{
    public IReadOnlyList<CuratedSelectionTopContributorItem> TopContributors { get; init; } = [];
    public string DominantSourceSummary { get; init; } = string.Empty;
    public string Methodology { get; init; } = string.Empty;
}

public enum CuratedSelectionSourceMapCategory
{
    IncludedMeasured,
    Unsupported,
    NoData,
    ContextOnlyExcluded
}

public sealed class CuratedSelectionSourceMapItem
{
    public string NodeKey { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public CuratedSelectionSourceMapCategory Category { get; init; } = CuratedSelectionSourceMapCategory.Unsupported;
    public string CategoryLabel { get; init; } = string.Empty;
}

public sealed class CuratedSelectionSourceMapSummary
{
    public IReadOnlyList<CuratedSelectionSourceMapItem> Items { get; init; } = [];
    public int IncludedMeasuredCount { get; init; }
    public int UnsupportedCount { get; init; }
    public int NoDataCount { get; init; }
    public int ContextOnlyCount { get; init; }
    public string Summary { get; init; } = string.Empty;
}

public sealed class CuratedSelectionCoverageSummary
{
    public int SelectedNodeCount { get; init; }
    public int SupportedNodeCount { get; init; }
    public int UnsupportedNodeCount { get; init; }
    public int ContextOnlyNodeCount { get; init; }
    public int NoDataNodeCount { get; init; }
    public int IncludedNodeCount { get; init; }
}

public enum CuratedSelectionAnomalyStatus
{
    Normal,
    Attention,
    Suspicious,
    DataIssue
}

public sealed class CuratedSelectionOperationalHealthSummary
{
    public CuratedSelectionAnomalyStatus Status { get; init; } = CuratedSelectionAnomalyStatus.Normal;
    public string Summary { get; init; } = string.Empty;
    public int HighDeviationNodeCount { get; init; }
    public int ElevatedDeviationNodeCount { get; init; }
    public int SelectedNodeCount { get; init; }
    public int SupportedNodeCount { get; init; }
    public int IncludedNodeCount { get; init; }
    public int UnsupportedNodeCount { get; init; }
    public int ContextOnlyNodeCount { get; init; }
    public int NoDataNodeCount { get; init; }
    public bool HasWeakCoverage { get; init; }
    public bool HasMixedSignedSelection { get; init; }
    public bool HasAbruptAggregateShift { get; init; }
    public double SupportedSelectionRatio { get; init; }
    public double IncludedCoverageRatio { get; init; }
    public double? AbruptShiftRatio { get; init; }
    public IReadOnlyList<string> Signals { get; init; } = [];
}

public enum CuratedSelectionForecastStatus
{
    Unavailable,
    LimitedData,
    Stable,
    Watch,
    PoorFit
}

public sealed record CuratedSelectionForecastDiagnosticsSummary
{
    public CuratedSelectionForecastStatus Status { get; init; } = CuratedSelectionForecastStatus.Unavailable;
    public string Summary { get; init; } = string.Empty;
    public string ForecastPrinciple { get; init; } = string.Empty;
    public string Methodology { get; init; } = string.Empty;
    public string MetricsNote { get; init; } = string.Empty;
    public int SupportedNodeCount { get; init; }
    public int IncludedNodeCount { get; init; }
    public int ForecastProviderNodeCount { get; init; }
    public int ForecastMissingNodeCount { get; init; }
    public int ActualPointCount { get; init; }
    public int ForecastPointCount { get; init; }
    public int AlignedPointCount { get; init; }
    public double AlignmentCoverageRatio { get; init; }
    public double? MaeKw { get; init; }
    public double? RmseKw { get; init; }
    public double? BiasKw { get; init; }
    public double? WapePercent { get; init; }
    public bool HasMixedSignedSelection { get; init; }
    public bool UsesTargetLeakage { get; init; }
    public IReadOnlyList<string> Signals { get; init; } = [];
}

public enum CuratedSelectionLoadProfileMode
{
    HourOfDayAverage,
    IntervalSnapshotFallback
}

public sealed class CuratedSelectionLoadProfileBucket
{
    public int BucketIndex { get; init; }
    public string Label { get; init; } = string.Empty;
    public double AverageKw { get; init; }
    public int SampleCount { get; init; }
}

public sealed class CuratedSelectionLoadProfileSummary
{
    public bool IsAvailable { get; init; }
    public CuratedSelectionLoadProfileMode Mode { get; init; } = CuratedSelectionLoadProfileMode.HourOfDayAverage;
    public string ModeLabel { get; init; } = string.Empty;
    public bool IsFallback { get; init; }
    public int PointCount { get; init; }
    public int DistinctDayCount { get; init; }
    public bool HasMixedSigns { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string Methodology { get; init; } = string.Empty;
    public string DifferenceFromForecast { get; init; } = string.Empty;
    public string DifferenceFromMainChart { get; init; } = string.Empty;
    public IReadOnlyList<CuratedSelectionLoadProfileBucket> Buckets { get; init; } = [];
}

public sealed class CuratedSelectionLoadDurationCurvePoint
{
    public double DurationPercent { get; init; }
    public double DemandKw { get; init; }
}

public sealed class CuratedSelectionLoadDurationCurveSummary
{
    public bool IsAvailable { get; init; }
    public bool HasMixedSigns { get; init; }
    public CuratedPerformanceKpiState State { get; init; } = CuratedPerformanceKpiState.Unavailable;
    public string Unit { get; init; } = "kW";
    public string YAxisLabel { get; init; } = "Demand (kW)";
    public string StateReason { get; init; } = string.Empty;
    public string EvaluationBasis { get; init; } = string.Empty;
    public IReadOnlyList<string> Notes { get; init; } = [];
    public int PointCount { get; init; }
    public double? PeakDemandKw { get; init; }
    public double? AverageDemandKw { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string Methodology { get; init; } = string.Empty;
    public IReadOnlyList<CuratedSelectionLoadDurationCurvePoint> Points { get; init; } = [];
}

public enum CuratedPeakSignificanceLevel
{
    Low,
    Medium,
    High
}

public sealed class CuratedSelectionPeakEvent
{
    public string Label { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public double ValueKw { get; init; }
    public double MagnitudeKw { get; init; }
    public DateTime? TimestampUtc { get; init; }
}

public sealed class CuratedSelectionPeakAnalysisSummary
{
    public bool IsAvailable { get; init; }
    public bool HasMixedSigns { get; init; }
    public CuratedAggregateEnergyProfile EnergyProfile { get; init; } = CuratedAggregateEnergyProfile.Neutral;
    public CuratedPerformanceKpiState State { get; init; } = CuratedPerformanceKpiState.Unavailable;
    public string StateReason { get; init; } = string.Empty;
    public string EvaluationBasis { get; init; } = string.Empty;
    public IReadOnlyList<string> Notes { get; init; } = [];
    public CuratedSelectionPeakEvent DemandPeak { get; init; } = new() { Label = "Peak demand" };
    public CuratedSelectionPeakEvent GenerationPeak { get; init; } = new() { Label = "Peak generation/export" };
    public CuratedSelectionPeakEvent NetAbsolutePeak { get; init; } = new() { Label = "Peak net absolute event" };
    public double? TypicalMagnitudeKw { get; init; }
    public double? SignificanceRatio { get; init; }
    public CuratedPeakSignificanceLevel SignificanceLevel { get; init; } = CuratedPeakSignificanceLevel.Low;
    public string Summary { get; init; } = string.Empty;
    public string Methodology { get; init; } = string.Empty;
}

public sealed class CuratedSelectionLoadFactorSummary
{
    public bool IsAvailable { get; init; }
    public bool HasMixedSigns { get; init; }
    public CuratedPerformanceKpiState State { get; init; } = CuratedPerformanceKpiState.Unavailable;
    public string StateReason { get; init; } = string.Empty;
    public string EvaluationBasis { get; init; } = string.Empty;
    public IReadOnlyList<string> Notes { get; init; } = [];
    public double? AverageDemandKw { get; init; }
    public double? PeakDemandKw { get; init; }
    public double? LoadFactorRatio { get; init; }
    public int PointCount { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string Methodology { get; init; } = string.Empty;
}

public sealed class CuratedSelectionAfterHoursLoadSummary
{
    public bool IsAvailable { get; init; }
    public bool HasMixedSigns { get; init; }
    public CuratedPerformanceKpiState State { get; init; } = CuratedPerformanceKpiState.Unavailable;
    public string StateReason { get; init; } = string.Empty;
    public string EvaluationBasis { get; init; } = string.Empty;
    public IReadOnlyList<string> Notes { get; init; } = [];
    public double? AverageActiveWeekdayDemandKw { get; init; }
    public double? AverageAfterHoursDemandKw { get; init; }
    public double? AfterHoursRatio { get; init; }
    public double? AverageNightDemandKw { get; init; }
    public double? NightRatio { get; init; }
    public double? AverageWeekendDemandKw { get; init; }
    public double? WeekendRatio { get; init; }
    public bool UsesReferenceFloor { get; init; }
    public int ActiveWeekdaySampleCount { get; init; }
    public int AfterHoursSampleCount { get; init; }
    public int NightSampleCount { get; init; }
    public int WeekendSampleCount { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string Methodology { get; init; } = string.Empty;
}

public enum CuratedPerformanceKpiState
{
    Unavailable,
    Indicative,
    Available
}

public sealed class CuratedSelectionOperatingRegimeSummary
{
    public bool IsAvailable { get; init; }
    public bool HasMixedSigns { get; init; }
    public double? BaseloadKw { get; init; }
    public double? AverageAbsoluteKw { get; init; }
    public double? PeakAbsoluteKw { get; init; }
    public double? PeakToAverageRatio { get; init; }
    public double? VariabilityCoefficient { get; init; }
    public double? WeekdayWeekendDeltaPercent { get; init; }
    public int WeekdaySampleCount { get; init; }
    public int WeekendSampleCount { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string Methodology { get; init; } = string.Empty;
    public IReadOnlyList<string> Signals { get; init; } = [];
}

public enum CuratedOperationalScorecardStatus
{
    Unavailable,
    Good,
    Watch,
    Issue
}

public sealed class CuratedSelectionOperationalScorecard
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public CuratedOperationalScorecardStatus Status { get; init; } = CuratedOperationalScorecardStatus.Unavailable;
    public double? MetricValue { get; init; }
    public string MetricDisplay { get; init; } = "N/A";
    public string MetricLabel { get; init; } = string.Empty;
    public string Thresholds { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Methodology { get; init; } = string.Empty;
}

public enum CuratedSelectionInefficiencySeverity
{
    None,
    Watch,
    Issue
}

public sealed class CuratedSelectionInefficiencyItem
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public CuratedSelectionInefficiencySeverity Severity { get; init; } = CuratedSelectionInefficiencySeverity.None;
    public bool IsTriggered { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string Evidence { get; init; } = string.Empty;
    public string Methodology { get; init; } = string.Empty;
}

public sealed class CuratedSelectionEmsEvaluationSummary
{
    public bool IsAvailable { get; init; }
    public bool HasMixedSigns { get; init; }
    public bool HasConsumption { get; init; }
    public bool HasGeneration { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string Methodology { get; init; } = string.Empty;
    public string DistinctionNote { get; init; } = string.Empty;
    public IReadOnlyList<CuratedSelectionOperationalScorecard> Scorecards { get; init; } = [];
    public IReadOnlyList<CuratedSelectionInefficiencyItem> Inefficiencies { get; init; } = [];
    public IReadOnlyList<string> Opportunities { get; init; } = [];
}

public enum CuratedEnergyContributionRole
{
    Consumption,
    Generation,
    MixedSigned,
    Neutral
}

public enum CuratedAggregateEnergyProfile
{
    ConsumptionOnly,
    GenerationOnly,
    MixedSigned,
    Neutral
}

public enum OverviewAggregateSemanticsMode
{
    Net,
    Consumption,
    Production
}

public enum CuratedNodeTimeSeriesMode
{
    Auto,
    Raw15Min,
    HourlyAverage,
    DailyAverage
}

public enum CuratedNodeTimeSeriesGranularity
{
    Raw15Min,
    HourlyAverage,
    DailyAverage
}

public enum NodeDeviationSeverity
{
    Normal,
    Elevated,
    High
}

public class CuratedNodeDeviationSummary
{
    public bool IsAvailable { get; set; }
    public double? CurrentValue { get; set; }
    public double? BaselineValue { get; set; }
    public double? DeltaAbsolute { get; set; }
    public double? DeltaPercent { get; set; }
    public NodeDeviationSeverity? Severity { get; set; }
    public int ReferenceIntervalsUsed { get; set; }
    public string Unit { get; set; } = "kWh";
    public string Methodology { get; set; } = string.Empty;
    public string? Message { get; set; }
    public WeatherExplanationSummary? WeatherExplanation { get; set; }
}

public enum WeatherExplanationStatus
{
    SupportedByWeather,
    NotSupportedByWeather,
    WeatherChangeNeutral,
    Unavailable
}

public sealed class WeatherExplanationSummary
{
    public bool IsAvailable { get; init; }
    public WeatherExplanationStatus Status { get; init; } = WeatherExplanationStatus.Unavailable;
    public double? CurrentAverageOutdoorTempC { get; init; }
    public double? ReferenceAverageOutdoorTempC { get; init; }
    public double? DeltaOutdoorTempC { get; init; }
    public string Conclusion { get; init; } = string.Empty;
    public string Methodology { get; init; } = string.Empty;
}

public class NodeAnalyticsPreviewService
{
    private const string BaselineMethodology = "The analysis window is always the user-selected interval. Baseline strategy is determined separately: priority goes to the same period in prior years, with fallback to previous comparable windows of the same length. For series with a P suffix, power is converted to energy before summation using the inferred time step, with a 15 min fallback.";
    private const string WeatherAwareBaselineMethodology = "Daily weather-aware baseline uses full UTC days only. Daily energy is taken directly from energy counters or derived from power by interval integration, then fitted against facility weather Ta with E_d = beta0 + betaH * HDD(18 C) + betaC * CDD(22 C). Diagnostics report CV(RMSE) and NMBE on the fit days used by the model.";
    private const string EuiMethodology = "EUI = Energy / Floor area. This MVP reports period EUI over the selected interval, not an annualized EUI. Floor area must be explicitly entered on an area node, and power-derived energy is integrated over actual timestamp spacing before normalization to kWh.";
    private const string TemperatureLoadScatterMethodology = "Temperature vs load scatter uses complete UTC hours only. X axis = facility outdoor-air temperature Ta. Y axis = hourly average power for power signals, or hourly energy-derived load for energy signals. Energy counters use the existing derived interval semantics instead of raw counter values. No fallback signal is used.";
    private const double DefaultPowerSampleStepHours = 0.25;
    private const int MaxHistoricalYearsForBaseline = 3;
    private const int RecentComparableWindowsForBaseline = 4;
    private const double MinimumReferenceCoverageRatio = 0.60;
    private const double WeatherExplanationDeltaThresholdC = 0.8;
    private const double WeatherAwareHeatingBalanceTemperatureC = 18.0;
    private const double WeatherAwareCoolingBalanceTemperatureC = 22.0;
    private const int WeatherAwareBaselineFitLookbackDays = 365;
    private const int WeatherAwareBaselineMinimumFitDays = 21;
    private const double WeatherAwareBaselineMinimumDailyCoverageRatio = 0.75;
    private const double TemperatureLoadScatterMinimumHourlyCoverageRatio = 0.75;
    private static readonly TimeSpan RawTimeSeriesThreshold = TimeSpan.FromDays(7);
    private static readonly TimeSpan HourlyTimeSeriesThreshold = TimeSpan.FromDays(45);

    private readonly IKpiService _kpiService;
    private readonly IWebHostEnvironment _env;
    private readonly FacilityDataBindingRegistry _bindingRegistry;
    private readonly FacilityEditorStateService _facilityEditorStateService;
    private readonly FacilityQueryService _facilityQueryService;
    private readonly FacilityWeatherSourceResolver _weatherSourceResolver;
    private readonly ConcurrentDictionary<string, DateTime> _maxTimestampCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, (DateTime MinUtc, DateTime MaxUtc)> _timeDomainCache = new(StringComparer.OrdinalIgnoreCase);

    public NodeAnalyticsPreviewService(
        IKpiService kpiService,
        IWebHostEnvironment env,
        FacilityDataBindingRegistry bindingRegistry,
        FacilityEditorStateService facilityEditorStateService,
        FacilityQueryService facilityQueryService,
        FacilityWeatherSourceResolver weatherSourceResolver)
    {
        _kpiService = kpiService;
        _env = env;
        _bindingRegistry = bindingRegistry;
        _facilityEditorStateService = facilityEditorStateService;
        _facilityQueryService = facilityQueryService;
        _weatherSourceResolver = weatherSourceResolver;
    }

    public async Task<MeterKpiResult?> GetPreviewDataAsync(string meterUrn, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var query = new KpiQuery(meterUrn, from, to);
        var result = await _kpiService.CalculateBasicKpiAsync(query, ct);

        // Pokud nemĂˇme ĹľĂˇdnĂˇ data, vracĂ­me null
        if (result.RecordCount == 0)
            return null;

        return result;
    }

    public bool SupportsComparePreview(string? nodeKey)
    {
        return !string.IsNullOrWhiteSpace(nodeKey) && _bindingRegistry.IsSupported(nodeKey);
    }

    public SelectionSignalAvailabilityResult GetSelectionSignalAvailability(IEnumerable<string> nodeKeys)
    {
        var distinctNodeKeys = nodeKeys?
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        if (distinctNodeKeys.Count == 0)
        {
            return new SelectionSignalAvailabilityResult
            {
                Message = "Select a node or a subtree to inspect available analytics signals."
            };
        }

        var bindingContexts = new List<SignalBindingContext>();

        foreach (var nodeKey in distinctNodeKeys)
        {
            var exactSignalCodes = _bindingRegistry.GetBindings(nodeKey)
                .Where(binding => !binding.ExactSignalCode.IsEmpty)
                .GroupBy(binding => binding.ExactSignalCode.Value, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First().ExactSignalCode)
                .ToList();

            foreach (var exactSignalCode in exactSignalCodes)
            {
                var binding = _bindingRegistry.GetPreferredBinding(nodeKey, exactSignalCode);
                var source = ResolveCuratedNodeSource(nodeKey, exactSignalCode);
                if (binding is null || source is null)
                {
                    continue;
                }

                bindingContexts.Add(new SignalBindingContext(nodeKey, binding, source));
            }
        }

        if (bindingContexts.Count == 0)
        {
            return new SelectionSignalAvailabilityResult
            {
                Message = "No exact signal bindings are available in the current analytics scope."
            };
        }

        var options = bindingContexts
            .GroupBy(context => context.Binding.ExactSignalCode.Value, StringComparer.OrdinalIgnoreCase)
            .Select(group => BuildSelectionSignalAvailabilityItem(group, distinctNodeKeys.Count))
            .OrderBy(option => option.SignalFamily.ToString(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(option => option.ExactSignalCode.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SelectionSignalAvailabilityResult
        {
            Options = options,
            Message = options.Count == 0
                ? "No exact signal bindings are available in the current analytics scope."
                : null
        };
    }

    public async Task<SelectionSignalAnalyticsResult> GetSelectionSignalAnalyticsAsync(
        IEnumerable<string> nodeKeys,
        FacilitySignalCode exactSignalCode,
        DateTime from,
        DateTime to,
        CuratedNodeTimeSeriesMode mode = CuratedNodeTimeSeriesMode.Auto,
        string? scopeAnchorNodeKey = null,
        CancellationToken ct = default)
    {
        if (exactSignalCode.IsEmpty)
        {
            return new SelectionSignalAnalyticsResult
            {
                Message = "Choose an exact signal code first."
            };
        }

        var scopeNodeKeys = nodeKeys?
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        var availability = GetSelectionSignalAvailability(scopeNodeKeys);
        var option = availability.Options.FirstOrDefault(item => FacilitySignalTaxonomy.MatchesExactCode(item.ExactSignalCode, exactSignalCode));

        if (option is null)
        {
            return new SelectionSignalAnalyticsResult
            {
                Message = availability.Message ?? "The selected signal is not available in the current analytics scope."
            };
        }

        if (option.CanRenderSingleNodeSeries)
        {
            var nodeKey = option.MatchingNodeKeys.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(nodeKey))
            {
                return new SelectionSignalAnalyticsResult
                {
                    SelectionSignal = option,
                    Message = "The selected signal does not resolve to a usable node in the current scope."
                };
            }

            var euiTask = BuildSelectionEuiAsync(option, scopeNodeKeys, scopeAnchorNodeKey, from, to, ct);
            var baselineTask = BuildSelectionWeatherAwareBaselineAsync(option, from, to, ct);
            var scatterTask = BuildSelectionTemperatureLoadScatterAsync(option, from, to, ct);
            var timeSeries = await GetCuratedTimeSeriesAsync(
                nodeKey,
                exactSignalCode,
                from,
                to,
                mode,
                includeBaselineOverlay: false,
                ct);
            var eui = await euiTask;
            var weatherAwareBaseline = await baselineTask;
            var temperatureLoadScatter = await scatterTask;

            return new SelectionSignalAnalyticsResult
            {
                SelectionSignal = option,
                TimeSeries = timeSeries,
                BasicStats = BuildSelectionSignalBasicStats(timeSeries),
                PowerAnalytics = BuildSelectionPowerAnalytics(option, timeSeries),
                Eui = eui,
                WeatherAwareBaseline = weatherAwareBaseline,
                TemperatureLoadScatter = temperatureLoadScatter,
                IsAvailable = timeSeries is not null && timeSeries.Points.Count > 0,
                IsAggregate = false,
                Message = timeSeries is null
                    ? "The selected signal cannot be resolved to a time-series source."
                    : timeSeries.Points.Count == 0
                        ? timeSeries.NoDataMessage ?? "No time-series data is available for the selected signal in the current interval."
                        : option.AvailabilityMessage,
                ContributingNodeKeys = [nodeKey]
            };
        }

        if (!option.CanAggregate)
        {
            var eui = await BuildSelectionEuiAsync(option, scopeNodeKeys, scopeAnchorNodeKey, from, to, ct);
            var weatherAwareBaseline = await BuildSelectionWeatherAwareBaselineAsync(option, from, to, ct);
            var temperatureLoadScatter = await BuildSelectionTemperatureLoadScatterAsync(option, from, to, ct);

            return new SelectionSignalAnalyticsResult
            {
                SelectionSignal = option,
                PowerAnalytics = BuildSelectionPowerAnalytics(option, null),
                Eui = eui,
                WeatherAwareBaseline = weatherAwareBaseline,
                TemperatureLoadScatter = temperatureLoadScatter,
                IsAvailable = false,
                IsAggregate = true,
                Message = option.AvailabilityMessage,
                ContributingNodeKeys = option.MatchingNodeKeys
            };
        }

        var euiTaskForAggregate = BuildSelectionEuiAsync(option, scopeNodeKeys, scopeAnchorNodeKey, from, to, ct);
        var baselineTaskForAggregate = BuildSelectionWeatherAwareBaselineAsync(option, from, to, ct);
        var scatterTaskForAggregate = BuildSelectionTemperatureLoadScatterAsync(option, from, to, ct);
        var aggregateTimeSeries = await GetSelectionSignalAggregateTimeSeriesAsync(option, from, to, mode, ct);
        var aggregateEui = await euiTaskForAggregate;
        var aggregateWeatherAwareBaseline = await baselineTaskForAggregate;
        var aggregateTemperatureLoadScatter = await scatterTaskForAggregate;

        return new SelectionSignalAnalyticsResult
        {
            SelectionSignal = option,
            TimeSeries = aggregateTimeSeries,
            BasicStats = BuildSelectionSignalBasicStats(aggregateTimeSeries),
            PowerAnalytics = BuildSelectionPowerAnalytics(option, aggregateTimeSeries),
            Eui = aggregateEui,
            WeatherAwareBaseline = aggregateWeatherAwareBaseline,
            TemperatureLoadScatter = aggregateTemperatureLoadScatter,
            IsAvailable = aggregateTimeSeries is not null && aggregateTimeSeries.Points.Count > 0,
            IsAggregate = true,
            Message = aggregateTimeSeries is null
                ? "The aggregate trend could not be resolved for the selected signal."
                : aggregateTimeSeries.Points.Count == 0
                    ? aggregateTimeSeries.NoDataMessage ?? "No time-series data is available for the selected signal in the current interval."
                    : option.AvailabilityMessage,
            ContributingNodeKeys = option.MatchingNodeKeys
        };
    }

    public async Task<CuratedNodeSummary?> GetCuratedSummaryAsync(string nodeKey, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var source = ResolveCuratedNodeSource(nodeKey);
        if (source is null)
        {
            return null;
        }

        var filePath = ResolveCuratedFilePath(source);
        if (filePath is null)
        {
            return null;
        }

        return await ParseCsvColumnAsync(filePath, source, from, to, ct);
    }

    public async Task<CuratedNodeTimeSeriesResult?> GetCuratedTimeSeriesAsync(
        string nodeKey,
        DateTime from,
        DateTime to,
        CuratedNodeTimeSeriesMode mode = CuratedNodeTimeSeriesMode.Auto,
        bool includeBaselineOverlay = false,
        CancellationToken ct = default)
        => await GetCuratedTimeSeriesCoreAsync(
            nodeKey,
            ResolveCuratedNodeSource(nodeKey),
            from,
            to,
            mode,
            includeBaselineOverlay,
            ct);

    public async Task<CuratedNodeTimeSeriesResult?> GetCuratedTimeSeriesAsync(
        string nodeKey,
        FacilitySignalCode exactSignalCode,
        DateTime from,
        DateTime to,
        CuratedNodeTimeSeriesMode mode = CuratedNodeTimeSeriesMode.Auto,
        bool includeBaselineOverlay = false,
        CancellationToken ct = default)
        => exactSignalCode.IsEmpty
            ? null
            : await GetCuratedTimeSeriesCoreAsync(
                nodeKey,
                ResolveCuratedNodeSource(nodeKey, exactSignalCode),
                from,
                to,
                mode,
                includeBaselineOverlay,
                ct);

    private async Task<CuratedNodeTimeSeriesResult?> GetCuratedTimeSeriesCoreAsync(
        string nodeKey,
        CuratedNodeSource? source,
        DateTime from,
        DateTime to,
        CuratedNodeTimeSeriesMode mode,
        bool includeBaselineOverlay,
        CancellationToken ct)
    {
        if (source is null)
        {
            return null;
        }

        var filePath = ResolveCuratedFilePath(source);
        if (filePath is null)
        {
            var granularity = ResolveTimeSeriesGranularity(from, to, source, mode);
            return new CuratedNodeTimeSeriesResult
            {
                NodeKey = nodeKey,
                Title = source.Title,
                Unit = ResolveTimeSeriesUnit(source),
                YAxisLabel = ResolveTimeSeriesYAxisLabel(source),
                SeriesSemantics = source.SeriesSemantics,
                UsesDerivedIntervalSeries = source.SeriesSemantics == FacilitySignalSeriesSemantics.CumulativeCounter,
                SeriesStatusMessage = ResolveSeriesStatusMessage(source, granularity.Granularity),
                Granularity = granularity.Granularity,
                GranularityLabel = granularity.Label,
                AggregationMethod = granularity.AggregationMethod,
                InterpretationNote = ResolveTimeSeriesInterpretationNote(source, granularity),
                RequestedMode = granularity.RequestedMode,
                RequestedModeLabel = granularity.RequestedModeLabel,
                BaselineOverlayRequested = includeBaselineOverlay,
                BaselineOverlayAvailable = false,
                BaselineOverlayMessage = includeBaselineOverlay
                    ? (source.SupportsDeviation
                        ? "Baseline overlay is unavailable because the local reduced source is missing."
                        : "Baseline overlay is not supported for this node.")
                    : null,
                NoDataMessage = "The local reduced source required to render this time series is missing."
            };
        }

        return await ParseCuratedTimeSeriesAsync(nodeKey, filePath, source, from, to, mode, includeBaselineOverlay, ct);
    }

    public async Task<CuratedNodeTimeSeriesResult?> GetCuratedSelectionAggregateTimeSeriesAsync(
        IEnumerable<string> selectedNodeKeys,
        DateTime from,
        DateTime to,
        CuratedNodeTimeSeriesMode mode = CuratedNodeTimeSeriesMode.Auto,
        bool includeBaselineOverlay = false,
        OverviewAggregateSemanticsMode semanticsMode = OverviewAggregateSemanticsMode.Net,
        CancellationToken ct = default)
    {
        selectedNodeKeys ??= [];

        var supportedNodeKeys = selectedNodeKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(nodeKey => ResolveCuratedNodeSource(nodeKey)?.IsPowerSignal == true)
            .ToList();

        if (supportedNodeKeys.Count == 0)
        {
            return null;
        }

        var nodeTasks = supportedNodeKeys
            .Select(nodeKey => GetCuratedTimeSeriesAsync(nodeKey, from, to, mode, includeBaselineOverlay, ct))
            .ToArray();

        await Task.WhenAll(nodeTasks);

        var timeSeries = nodeTasks
            .Select(task => task.Result)
            .Where(result => result is not null && result.Points.Count > 0)
            .Cast<CuratedNodeTimeSeriesResult>()
            .ToList();

        if (timeSeries.Count == 0)
        {
            return null;
        }

        var template = timeSeries[0];
        var points = BuildOverviewAggregatePoints(timeSeries.Select(x => x.Points), semanticsMode);
        var baselineProviders = timeSeries.Count(x => x.BaselinePoints.Count > 0);
        var baselinePoints = includeBaselineOverlay
            ? BuildOverviewAggregatePoints(timeSeries.Where(x => x.BaselinePoints.Count > 0).Select(x => x.BaselinePoints), semanticsMode)
            : [];

        string? baselineOverlayMessage = null;
        if (includeBaselineOverlay)
        {
            if (baselineProviders == 0)
            {
                baselineOverlayMessage = "Baseline overlay is unavailable in aggregate mode for all supported nodes.";
            }
            else if (baselineProviders < timeSeries.Count)
            {
                baselineOverlayMessage = $"Baseline overlay is aggregated from {baselineProviders}/{timeSeries.Count} supported nodes.";
            }
        }

        return new CuratedNodeTimeSeriesResult
        {
            NodeKey = "selection_set",
            Title = ResolveOverviewAggregateTitle(supportedNodeKeys.Count, template.Title, semanticsMode),
            Unit = template.Unit,
            YAxisLabel = template.YAxisLabel,
            Granularity = template.Granularity,
            GranularityLabel = template.GranularityLabel,
            AggregationMethod = ResolveOverviewAggregateAggregationMethod(template.AggregationMethod, semanticsMode),
            InterpretationNote = ResolveOverviewAggregateInterpretationNote(template.Granularity, semanticsMode),
            RequestedMode = template.RequestedMode,
            RequestedModeLabel = template.RequestedModeLabel,
            BaselineOverlayRequested = includeBaselineOverlay,
            BaselineOverlayAvailable = baselinePoints.Count > 0,
            BaselineOverlayMessage = baselineOverlayMessage,
            BaselinePoints = baselinePoints,
            NoDataMessage = points.Count == 0 ? ResolveOverviewAggregateNoDataMessage(semanticsMode) : null,
            Points = points
        };
    }

    private static IReadOnlyList<CuratedNodeTimeSeriesPoint> BuildOverviewAggregatePoints(
        IEnumerable<IReadOnlyList<CuratedNodeTimeSeriesPoint>> seriesCollection,
        OverviewAggregateSemanticsMode semanticsMode)
    {
        return seriesCollection
            .SelectMany(points => points)
            .GroupBy(point => point.TimestampUtc)
            .Select(group =>
            {
                var values = group.Select(point => point.Value).ToArray();
                var aggregateValue = semanticsMode switch
                {
                    OverviewAggregateSemanticsMode.Consumption => values.Where(value => value > 0d).Sum(),
                    OverviewAggregateSemanticsMode.Production => values.Where(value => value < 0d).Sum(value => Math.Abs(value)),
                    _ => values.Sum()
                };

                return new CuratedNodeTimeSeriesPoint
                {
                    TimestampUtc = group.Key,
                    Value = aggregateValue
                };
            })
            .OrderBy(point => point.TimestampUtc)
            .ToList();
    }

    private static string ResolveOverviewAggregateTitle(
        int supportedNodeCount,
        string templateTitle,
        OverviewAggregateSemanticsMode semanticsMode)
    {
        if (supportedNodeCount == 1)
        {
            return templateTitle;
        }

        return semanticsMode switch
        {
            OverviewAggregateSemanticsMode.Consumption => "Selection set consumption view",
            OverviewAggregateSemanticsMode.Production => "Selection set production view",
            _ => "Selection set net view"
        };
    }

    private static string ResolveOverviewAggregateAggregationMethod(string baseAggregationMethod, OverviewAggregateSemanticsMode semanticsMode)
    {
        var semanticsText = semanticsMode switch
        {
            OverviewAggregateSemanticsMode.Consumption => "positive supported-node power only at each timestamp",
            OverviewAggregateSemanticsMode.Production => "absolute value of negative supported-node power only at each timestamp",
            _ => "signed sum of supported-node power at each timestamp"
        };

        return $"{baseAggregationMethod} Selection aggregate: {semanticsText}.";
    }

    private static string ResolveOverviewAggregateInterpretationNote(
        CuratedNodeTimeSeriesGranularity granularity,
        OverviewAggregateSemanticsMode semanticsMode)
    {
        var granularityText = granularity switch
        {
            CuratedNodeTimeSeriesGranularity.DailyAverage => "Daily aggregation smooths intra-day variation.",
            CuratedNodeTimeSeriesGranularity.HourlyAverage => "Hourly aggregation shows the dominant daily shape.",
            _ => "Raw detail keeps the original intra-interval variation."
        };

        var semanticsText = semanticsMode switch
        {
            OverviewAggregateSemanticsMode.Consumption => "Consumption shows only load-side contribution; production is excluded from the plotted values.",
            OverviewAggregateSemanticsMode.Production => "Production shows only generation-side contribution as positive magnitude.",
            _ => "Net shows the signed balance of consumption and production."
        };

        return $"{granularityText} {semanticsText}";
    }

    private static string ResolveOverviewAggregateNoDataMessage(OverviewAggregateSemanticsMode semanticsMode)
        => semanticsMode switch
        {
            OverviewAggregateSemanticsMode.Consumption => "No consumption-basis time-series data is available for the selected interval.",
            OverviewAggregateSemanticsMode.Production => "No production-basis time-series data is available for the selected interval.",
            _ => "No net time-series data is available for the selected interval."
        };

    public async Task<CuratedSelectionAggregateOverviewResult> GetCuratedSelectionAggregateOverviewAsync(
        IEnumerable<string> selectedNodeKeys,
        DateTime from,
        DateTime to,
        CuratedNodeTimeSeriesMode mode = CuratedNodeTimeSeriesMode.Auto,
        bool includeBaselineOverlay = false,
        CuratedSelectionAggregateRequestOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new CuratedSelectionAggregateRequestOptions();
        selectedNodeKeys ??= [];

        var distinctNodeKeys = selectedNodeKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var supportedNodeKeys = new List<string>();
        var unsupportedNodeKeys = new List<string>();
        var contextOnlyNodeKeys = new List<string>();

        foreach (var nodeKey in distinctNodeKeys)
        {
            var source = ResolveCuratedNodeSource(nodeKey);
            if (source is null)
            {
                unsupportedNodeKeys.Add(nodeKey);
                continue;
            }

            if (source.IsPowerSignal)
            {
                supportedNodeKeys.Add(nodeKey);
            }
            else
            {
                contextOnlyNodeKeys.Add(nodeKey);
            }
        }

        var sourceMapLabelMap = distinctNodeKeys.ToDictionary(
            nodeKey => nodeKey,
            nodeKey => ResolveCuratedNodeSource(nodeKey)?.Title ?? nodeKey,
            StringComparer.OrdinalIgnoreCase);

        if (supportedNodeKeys.Count == 0)
        {
            var emptyCoverage = new CuratedSelectionCoverageSummary
            {
                SelectedNodeCount = distinctNodeKeys.Count,
                SupportedNodeCount = 0,
                UnsupportedNodeCount = unsupportedNodeKeys.Count,
                ContextOnlyNodeCount = contextOnlyNodeKeys.Count,
                NoDataNodeCount = 0,
                IncludedNodeCount = 0
            };

            var emptyDisaggregation = BuildDisaggregationSummary(Array.Empty<CuratedSelectionContributionItem>(), CuratedAggregateEnergyProfile.Neutral);
            var emptyContributionIntelligence = BuildContributionIntelligenceSummary(Array.Empty<CuratedSelectionContributionItem>(), CuratedAggregateEnergyProfile.Neutral);
            var emptySourceMap = BuildSourceMapSummary(
                [],
                unsupportedNodeKeys,
                [],
                contextOnlyNodeKeys,
                sourceMapLabelMap);

            return new CuratedSelectionAggregateOverviewResult
            {
                SupportedNodeKeys = [],
                UnsupportedNodeKeys = unsupportedNodeKeys,
                ContextOnlyNodeKeys = contextOnlyNodeKeys,
                NoDataNodeKeys = [],
                IncludedNodeKeys = [],
                LoadProfile = new CuratedSelectionLoadProfileSummary
                {
                    IsAvailable = false,
                    Summary = "Load profile is unavailable because the selection set contains no supported energy nodes.",
                    Methodology = "Daily profile v1 uses transparent aggregation of the selection-set power series, with an hour-of-day average or an interval-snapshot fallback.",
                    DifferenceFromForecast = "Forecast predicts future behavior, while the load profile describes typical historical behavior.",
                    DifferenceFromMainChart = "The main chart shows the course of the selected interval, while the load profile aggregates repeating patterns across it."
                },
                PeakAnalysis = new CuratedSelectionPeakAnalysisSummary
                {
                    IsAvailable = false,
                    Summary = "Peak analysis is unavailable without supported nodes.",
                    Methodology = "Peak analysis v1 evaluates peak demand, peak generation/export, and the peak net absolute event from the aggregate power series."
                },
                OperatingRegime = new CuratedSelectionOperatingRegimeSummary
                {
                    IsAvailable = false,
                    Summary = "Operating regime summary is unavailable without an aggregate time series.",
                    Methodology = "Operating regime v1 uses transparent heuristics over aggregate power: baseload proxy, peak-to-average, variability, and a weekday/weekend signal."
                },
                EmsEvaluation = new CuratedSelectionEmsEvaluationSummary
                {
                    IsAvailable = false,
                    Summary = "EMS evaluation is unavailable because the selection set contains no supported energy nodes.",
                    Methodology = "EMS evaluation v1 builds transparent scorecards only on aggregate load-profile, peak, and operating-regime metrics.",
                    DistinctionNote = "Issue = a data or coverage problem, anomaly = a baseline deviation, inefficiency = persistent operational inefficiency."
                },
                Disaggregation = emptyDisaggregation,
                ContributionIntelligence = emptyContributionIntelligence,
                SourceMap = emptySourceMap,
                Coverage = emptyCoverage,
                OperationalHealth = BuildOperationalHealthSummary(
                    emptyCoverage,
                    CuratedAggregateEnergyProfile.Neutral,
                    [],
                    null),
                ForecastDiagnostics = new CuratedSelectionForecastDiagnosticsSummary
                {
                    Status = CuratedSelectionForecastStatus.Unavailable,
                    Summary = "Forecast is unavailable because the selection set contains no supported energy nodes.",
                    ForecastPrinciple = "Comparable windows v1 (transparent)",
                    Methodology = "Forecast uses only historical reference windows before the target interval; unsupported nodes are excluded from the calculation.",
                    MetricsNote = "Diagnostic metrics such as MAE, RMSE, Bias, and WAPE are calculated only when both forecast and actual series are available.",
                    SupportedNodeCount = 0,
                    IncludedNodeCount = 0,
                    ForecastProviderNodeCount = 0,
                    ForecastMissingNodeCount = 0,
                    UsesTargetLeakage = false,
                    Signals = ["no_supported_nodes"]
                },
                Message = "The selection set contains no compatible energy nodes for aggregate analytics."
            };
        }

        var nodeTasks = supportedNodeKeys
            .Select(async nodeKey =>
            {
                var summaryTask = GetCuratedSummaryAsync(nodeKey, from, to, ct);
                // Forecast v1 uses the same transparent historical reference windows as baseline overlay,
                // but remains semantically separated from deviation alerts in the returned overview model.
                var timeSeriesTask = GetCuratedTimeSeriesAsync(nodeKey, from, to, mode, includeBaselineOverlay: true, ct);
                var deviationTask = GetCuratedDeviationSummaryAsync(nodeKey, from, to, ct);
                await Task.WhenAll(summaryTask, timeSeriesTask, deviationTask);

                return (NodeKey: nodeKey, Summary: summaryTask.Result, TimeSeries: timeSeriesTask.Result, Deviation: deviationTask.Result);
            })
            .ToArray();

        await Task.WhenAll(nodeTasks);

        var summaries = new List<CuratedNodeSummary>();
        var timeSeries = new List<CuratedNodeTimeSeriesResult>();
        var deviationSummaries = new List<CuratedNodeDeviationSummary>();
        var breakdownInputs = new List<(string NodeKey, string Label, FacilityNodeRole NodeRole, string NodeRoleLabel, double? IntervalEnergyKwh)>();

        foreach (var task in nodeTasks)
        {
            var result = task.Result;
            var source = ResolveCuratedNodeSource(result.NodeKey);
            var displayLabel = !string.IsNullOrWhiteSpace(source?.Title)
                ? source!.Title
                : result.NodeKey;
            var nodeRole = FacilityNodeSemantics.ResolveRole(result.NodeKey, source?.NodeTypeHint, displayLabel);
            var nodeRoleLabel = FacilityNodeSemantics.GetRoleLabel(nodeRole);

            if (result.Summary is not null)
            {
                summaries.Add(result.Summary);
                breakdownInputs.Add((result.NodeKey, displayLabel, nodeRole, nodeRoleLabel, result.Summary.TotalSum));
            }
            else
            {
                breakdownInputs.Add((result.NodeKey, displayLabel, nodeRole, nodeRoleLabel, null));
            }

            if (result.TimeSeries is not null && result.TimeSeries.Points.Count > 0)
            {
                timeSeries.Add(result.TimeSeries);
            }

            deviationSummaries.Add(result.Deviation);
        }

        var availableBreakdownEnergies = breakdownInputs
            .Where(x => x.IntervalEnergyKwh.HasValue)
            .Select(x => x.IntervalEnergyKwh!.Value)
            .ToList();

        var hasPositiveContributions = availableBreakdownEnergies.Any(value => value > 0);
        var hasNegativeContributions = availableBreakdownEnergies.Any(value => value < 0);
        var energyProfile = ResolveAggregateEnergyProfile(hasPositiveContributions, hasNegativeContributions);

        var totalConsumptionKwh = availableBreakdownEnergies
            .Where(value => value > 0)
            .Sum();
        var totalGenerationKwh = availableBreakdownEnergies
            .Where(value => value < 0)
            .Sum(value => Math.Abs(value));
        var netEnergyKwh = availableBreakdownEnergies.Sum();
        var totalAbsoluteContributionKwh = availableBreakdownEnergies.Sum(value => Math.Abs(value));
        var headlineSemantics = ResolveHeadlineSemantics(energyProfile, totalConsumptionKwh, totalGenerationKwh, netEnergyKwh);

        var breakdown = breakdownInputs
            .Select(item =>
            {
                var hasData = item.IntervalEnergyKwh.HasValue;
                var energy = hasData ? item.IntervalEnergyKwh!.Value : 0d;
                var role = hasData
                    ? ResolveContributionRole(energy)
                    : CuratedEnergyContributionRole.Neutral;
                double? share = hasData && totalAbsoluteContributionKwh > 0
                    ? (Math.Abs(energy) / totalAbsoluteContributionKwh) * 100.0
                    : null;

                return new CuratedSelectionContributionItem
                {
                    NodeKey = item.NodeKey,
                    Label = item.Label,
                    NodeRole = item.NodeRole,
                    NodeRoleLabel = item.NodeRoleLabel,
                    HasData = hasData,
                    IntervalEnergyKwh = energy,
                    ContributionRole = role,
                    SharePercent = share
                };
            })
            .OrderByDescending(x => x.HasData)
            .ThenByDescending(x => Math.Abs(x.IntervalEnergyKwh))
            .ThenBy(x => x.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var noDataNodeKeys = breakdown
            .Where(item => !item.HasData)
            .Select(item => item.NodeKey)
            .ToList();

        var includedNodeKeys = breakdown
            .Where(item => item.HasData)
            .Select(item => item.NodeKey)
            .ToList();

        var roleBreakdown = breakdown
            .Where(item => !FacilityNodeSemantics.IsWeatherContextNode(item.NodeKey))
            .GroupBy(item => new { item.NodeRole, item.NodeRoleLabel })
            .Select(group =>
            {
                var contributingItems = group.Where(item => item.HasData).ToList();
                var hasData = contributingItems.Count > 0;
                var intervalEnergyKwh = hasData
                    ? contributingItems.Sum(item => item.IntervalEnergyKwh)
                    : 0d;
                var absoluteContributionKwh = hasData
                    ? contributingItems.Sum(item => Math.Abs(item.IntervalEnergyKwh))
                    : 0d;
                var hasPositiveContributionsInRole = contributingItems.Any(item => item.IntervalEnergyKwh > 0);
                var hasNegativeContributionsInRole = contributingItems.Any(item => item.IntervalEnergyKwh < 0);
                var hasMixedSigns = hasPositiveContributionsInRole && hasNegativeContributionsInRole;
                var contributionRole = hasMixedSigns
                    ? CuratedEnergyContributionRole.MixedSigned
                    : ResolveContributionRole(intervalEnergyKwh);
                double? sharePercent = hasData && totalAbsoluteContributionKwh > 0
                    ? (absoluteContributionKwh / totalAbsoluteContributionKwh) * 100.0
                    : null;

                return new CuratedSelectionRoleContributionItem
                {
                    Role = group.Key.NodeRole,
                    RoleLabel = group.Key.NodeRoleLabel,
                    NodeCount = group.Count(),
                    ContributingNodeCount = contributingItems.Count,
                    HasData = hasData,
                    HasMixedSigns = hasMixedSigns,
                    IntervalEnergyKwh = intervalEnergyKwh,
                    ContributionRole = contributionRole,
                    SharePercent = sharePercent
                };
            })
            .OrderByDescending(item => item.HasData)
            .ThenByDescending(item => Math.Abs(item.IntervalEnergyKwh))
            .ThenBy(item => item.RoleLabel, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var coverage = new CuratedSelectionCoverageSummary
        {
            SelectedNodeCount = distinctNodeKeys.Count,
            SupportedNodeCount = supportedNodeKeys.Count,
            UnsupportedNodeCount = unsupportedNodeKeys.Count,
            ContextOnlyNodeCount = contextOnlyNodeKeys.Count,
            NoDataNodeCount = noDataNodeKeys.Count,
            IncludedNodeCount = includedNodeKeys.Count
        };

        foreach (var item in breakdown)
        {
            sourceMapLabelMap[item.NodeKey] = item.Label;
        }

        var disaggregation = BuildDisaggregationSummary(breakdown, energyProfile);
        var contributionIntelligence = BuildContributionIntelligenceSummary(breakdown, energyProfile);
        var sourceMap = BuildSourceMapSummary(
            includedNodeKeys,
            unsupportedNodeKeys,
            noDataNodeKeys,
            contextOnlyNodeKeys,
            sourceMapLabelMap);

        CuratedNodeSummary? aggregateSummary = null;
        if (summaries.Count > 0)
        {
            var summaryTitle = supportedNodeKeys.Count == 1
                ? summaries[0].Title
                : "Selection set aggregate";
            var totalStatsSamples = summaries.Sum(x => x.DataPoints);
            var weightedAverage = totalStatsSamples > 0
                ? summaries.Sum(x => x.Average * x.DataPoints) / totalStatsSamples
                : summaries.Average(x => x.Average);

            aggregateSummary = new CuratedNodeSummary
            {
                Title = summaryTitle,
                TotalSum = netEnergyKwh,
                Average = weightedAverage,
                Min = summaries.Min(x => x.Min),
                Max = summaries.Max(x => x.Max),
                DataPoints = totalStatsSamples,
                Unit = "kWh",
                SummaryLabel = headlineSemantics.HeadlineLabel,
                StatsUnit = "kW",
                StatsLabel = "Aggregated power"
            };
        }

        CuratedNodeTimeSeriesResult? aggregateTimeSeries = null;
        if (timeSeries.Count > 0)
        {
            var template = timeSeries[0];
            var points = SumTimeSeriesByTimestamp(timeSeries.Select(x => x.Points));
            var baselineProviders = timeSeries.Count(x => x.BaselinePoints.Count > 0);
            var baselinePoints = includeBaselineOverlay
                ? SumTimeSeriesByTimestamp(timeSeries.Where(x => x.BaselinePoints.Count > 0).Select(x => x.BaselinePoints))
                : [];

            string? baselineOverlayMessage = null;
            if (includeBaselineOverlay)
            {
                if (baselineProviders == 0)
                {
                    baselineOverlayMessage = "Baseline overlay is unavailable in aggregate mode for all supported nodes.";
                }
                else if (baselineProviders < timeSeries.Count)
                {
                    baselineOverlayMessage = $"Baseline overlay is aggregated from {baselineProviders}/{timeSeries.Count} supported nodes.";
                }
            }

            aggregateTimeSeries = new CuratedNodeTimeSeriesResult
            {
                NodeKey = "selection_set",
                Title = supportedNodeKeys.Count == 1 ? template.Title : "Selection set aggregate power",
                Unit = template.Unit,
                YAxisLabel = template.YAxisLabel,
                Granularity = template.Granularity,
                GranularityLabel = template.GranularityLabel,
                AggregationMethod = $"{template.AggregationMethod} Selection aggregate: sum of supported-node power at each timestamp.",
                InterpretationNote = ResolveSelectionAggregateInterpretationNote(template.Granularity, energyProfile),
                RequestedMode = template.RequestedMode,
                RequestedModeLabel = template.RequestedModeLabel,
                BaselineOverlayRequested = includeBaselineOverlay,
                BaselineOverlayAvailable = baselinePoints.Count > 0,
                BaselineOverlayMessage = baselineOverlayMessage,
                BaselinePoints = baselinePoints,
                NoDataMessage = points.Count == 0
                    ? "No time-series points are available in the interval for supported selection nodes."
                    : null,
                Points = points
            };
        }

        CuratedNodeTimeSeriesResult? performanceEvaluationTimeSeries = null;
        if (options.IncludePerformance)
        {
            performanceEvaluationTimeSeries = await ResolvePerformanceEvaluationTimeSeriesAsync(
                aggregateTimeSeries,
                distinctNodeKeys,
                from,
                to,
                ct);
        }

        var forecastProviderNodeCount = timeSeries.Count(series => series.BaselinePoints.Count > 0);
        var aggregateForecastPoints = SumTimeSeriesByTimestamp(
            timeSeries
                .Where(series => series.BaselinePoints.Count > 0)
                .Select(series => series.BaselinePoints));

        var (forecastCompareTimeSeries, forecastDiagnostics) = BuildSelectionForecastOutputs(
            aggregateTimeSeries,
            aggregateForecastPoints,
            coverage,
            energyProfile,
            forecastProviderNodeCount);

        var messageParts = new List<string>();
        if (unsupportedNodeKeys.Count > 0)
        {
            messageParts.Add($"Unsupported nodes ignored: {unsupportedNodeKeys.Count}");
        }

        if (contextOnlyNodeKeys.Count > 0)
        {
            messageParts.Add($"Context-only or excluded nodes: {contextOnlyNodeKeys.Count}");
        }

        if (noDataNodeKeys.Count > 0)
        {
            messageParts.Add($"Nodes with no interval data: {noDataNodeKeys.Count}");
        }

        var message = messageParts.Count > 0
            ? string.Join(". ", messageParts) + "."
            : null;

        var operationalHealth = options.IncludePerformance
            ? BuildOperationalHealthSummary(
                coverage,
                energyProfile,
                deviationSummaries,
                aggregateTimeSeries)
            : new CuratedSelectionOperationalHealthSummary
            {
                SelectedNodeCount = coverage.SelectedNodeCount,
                SupportedNodeCount = coverage.SupportedNodeCount,
                IncludedNodeCount = coverage.IncludedNodeCount,
                UnsupportedNodeCount = coverage.UnsupportedNodeCount,
                ContextOnlyNodeCount = coverage.ContextOnlyNodeCount,
                NoDataNodeCount = coverage.NoDataNodeCount,
                SupportedSelectionRatio = coverage.SelectedNodeCount > 0
                    ? (double)coverage.SupportedNodeCount / coverage.SelectedNodeCount
                    : 0,
                IncludedCoverageRatio = coverage.SupportedNodeCount > 0
                    ? (double)coverage.IncludedNodeCount / coverage.SupportedNodeCount
                    : 0,
                Summary = "Operational Health loads when the Performance tab is opened."
            };
        var loadProfile = options.IncludePerformance
            ? BuildLoadProfileSummary(
                aggregateTimeSeries,
                from,
                to,
                energyProfile,
                includedNodeKeys.Count)
            : new CuratedSelectionLoadProfileSummary
            {
                Summary = "Load Profile loads when the Performance tab is opened."
            };
        var loadDurationCurve = options.IncludePerformance
            ? BuildLoadDurationCurveSummary(performanceEvaluationTimeSeries, energyProfile, to - from)
            : new CuratedSelectionLoadDurationCurveSummary
            {
                Summary = "Load Duration Curve loads when the Performance tab is opened."
            };
        var peakAnalysis = options.IncludePerformance
            ? BuildPeakAnalysisSummary(performanceEvaluationTimeSeries, energyProfile, to - from)
            : new CuratedSelectionPeakAnalysisSummary
            {
                Summary = "Peak Analysis loads when the Performance tab is opened."
            };
        var loadFactor = options.IncludePerformance
            ? BuildLoadFactorSummary(performanceEvaluationTimeSeries, energyProfile, to - from)
            : new CuratedSelectionLoadFactorSummary
            {
                Summary = "Load Factor loads when the Performance tab is opened."
            };
        var afterHoursLoad = options.IncludePerformance
            ? BuildAfterHoursLoadSummary(performanceEvaluationTimeSeries, energyProfile, to - from)
            : new CuratedSelectionAfterHoursLoadSummary
            {
                Summary = "After-hours Load loads when the Performance tab is opened."
            };
        var operatingRegime = options.IncludePerformance
            ? BuildOperatingRegimeSummary(aggregateTimeSeries, energyProfile)
            : new CuratedSelectionOperatingRegimeSummary
            {
                Summary = "Operating Regime loads when the Performance tab is opened."
            };
        var emsEvaluation = options.IncludePerformance
            ? BuildEmsEvaluationSummary(
                aggregateTimeSeries,
                loadProfile,
                peakAnalysis,
                operatingRegime,
                energyProfile,
                totalConsumptionKwh,
                totalGenerationKwh,
                netEnergyKwh)
            : new CuratedSelectionEmsEvaluationSummary
            {
                Summary = "EMS evaluation is not part of the phase 1 default load."
            };

        var effectiveBreakdown = options.IncludeBreakdown ? breakdown : [];
        var effectiveRoleBreakdown = options.IncludeBreakdown ? roleBreakdown : [];
        var effectiveDisaggregation = options.IncludeBreakdown
            ? disaggregation
            : new CuratedSelectionDisaggregationSummary
            {
                CompositionSummary = "Breakdown loads when the Breakdown tab is opened.",
                Methodology = "Disaggregation is computed only after the Breakdown tab is activated."
            };
        var effectiveContributionIntelligence = options.IncludeBreakdown
            ? contributionIntelligence
            : new CuratedSelectionContributionIntelligenceSummary();
        var effectiveSourceMap = options.IncludeBreakdown
            ? sourceMap
            : new CuratedSelectionSourceMapSummary
            {
                Summary = "Source Map loads when the Breakdown tab is opened.",
                IncludedMeasuredCount = includedNodeKeys.Count,
                UnsupportedCount = unsupportedNodeKeys.Count,
                NoDataCount = noDataNodeKeys.Count,
                ContextOnlyCount = contextOnlyNodeKeys.Count
            };

        var effectiveForecastCompareTimeSeries = options.IncludeDiagnostics
            ? forecastCompareTimeSeries
            : null;
        var effectiveForecastDiagnostics = options.IncludeDiagnostics
            ? forecastDiagnostics
            : new CuratedSelectionForecastDiagnosticsSummary
            {
                Summary = "Diagnostics load when the Diagnostics tab is opened.",
                SupportedNodeCount = coverage.SupportedNodeCount,
                IncludedNodeCount = coverage.IncludedNodeCount,
                ForecastProviderNodeCount = forecastProviderNodeCount,
                ForecastMissingNodeCount = Math.Max(0, coverage.IncludedNodeCount - forecastProviderNodeCount)
            };

        return new CuratedSelectionAggregateOverviewResult
        {
            Summary = aggregateSummary,
            TimeSeries = aggregateTimeSeries,
            LoadProfile = loadProfile,
            LoadDurationCurve = loadDurationCurve,
            PeakAnalysis = peakAnalysis,
            LoadFactor = loadFactor,
            AfterHoursLoad = afterHoursLoad,
            OperatingRegime = operatingRegime,
            EmsEvaluation = emsEvaluation,
            Breakdown = effectiveBreakdown,
            RoleBreakdown = effectiveRoleBreakdown,
            Disaggregation = effectiveDisaggregation,
            ContributionIntelligence = effectiveContributionIntelligence,
            SourceMap = effectiveSourceMap,
            Coverage = coverage,
            OperationalHealth = operationalHealth,
            ForecastCompareTimeSeries = effectiveForecastCompareTimeSeries,
            ForecastDiagnostics = effectiveForecastDiagnostics,
            EnergyProfile = energyProfile,
            HasNegativeContributions = hasNegativeContributions,
            TotalConsumptionKwh = totalConsumptionKwh,
            TotalGenerationKwh = totalGenerationKwh,
            NetEnergyKwh = netEnergyKwh,
            HeadlineValueKwh = headlineSemantics.HeadlineValueKwh,
            HeadlineLabel = headlineSemantics.HeadlineLabel,
            HeadlineDescription = headlineSemantics.HeadlineDescription,
            IsNetHeadline = headlineSemantics.IsNetHeadline,
            SupportedNodeKeys = supportedNodeKeys,
            UnsupportedNodeKeys = unsupportedNodeKeys,
            ContextOnlyNodeKeys = contextOnlyNodeKeys,
            NoDataNodeKeys = noDataNodeKeys,
            IncludedNodeKeys = includedNodeKeys,
            Message = message
        };
    }

    private async Task<CuratedNodeTimeSeriesResult?> ResolvePerformanceEvaluationTimeSeriesAsync(
        CuratedNodeTimeSeriesResult? aggregateTimeSeries,
        IReadOnlyList<string> selectedNodeKeys,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var performanceMode = ResolvePerformanceEvaluationMode(from, to);
        if (aggregateTimeSeries is not null && MatchesPerformanceEvaluationMode(aggregateTimeSeries, performanceMode))
        {
            return aggregateTimeSeries;
        }

        return await GetCuratedSelectionAggregateTimeSeriesAsync(selectedNodeKeys, from, to, performanceMode, includeBaselineOverlay: false, ct: ct);
    }

    private static (CuratedNodeCompareTimeSeriesResult? CompareTimeSeries, CuratedSelectionForecastDiagnosticsSummary Diagnostics) BuildSelectionForecastOutputs(
        CuratedNodeTimeSeriesResult? aggregateActualTimeSeries,
        IReadOnlyList<CuratedNodeTimeSeriesPoint> aggregateForecastPoints,
        CuratedSelectionCoverageSummary coverage,
        CuratedAggregateEnergyProfile energyProfile,
        int forecastProviderNodeCount)
    {
        var baseDiagnostics = new CuratedSelectionForecastDiagnosticsSummary
        {
            ForecastPrinciple = "Comparable windows v1 (transparent)",
            Methodology = "Forecast is constructed from historical reference windows, including the same period in prior years and recent comparable intervals. Only data available before the target interval is used, so no target-window leakage is introduced.",
            MetricsNote = "MAE, RMSE, and Bias are computed over signed power (kW). WAPE uses the sum of absolute actual values, so it remains robust for mixed-sign selections.",
            SupportedNodeCount = Math.Max(0, coverage.SupportedNodeCount),
            IncludedNodeCount = Math.Max(0, coverage.IncludedNodeCount),
            ForecastProviderNodeCount = Math.Max(0, forecastProviderNodeCount),
            ForecastMissingNodeCount = Math.Max(0, Math.Max(0, coverage.IncludedNodeCount) - Math.Max(0, forecastProviderNodeCount)),
            HasMixedSignedSelection = energyProfile == CuratedAggregateEnergyProfile.MixedSigned,
            UsesTargetLeakage = false
        };

        if (aggregateActualTimeSeries is null || aggregateActualTimeSeries.Points.Count == 0)
        {
            return (null, baseDiagnostics with
            {
                Status = CuratedSelectionForecastStatus.Unavailable,
                Summary = "Forecast vs actual cannot be evaluated because the aggregate actual series has no points.",
                Signals = ["missing_actual_series"]
            });
        }

        if (aggregateForecastPoints.Count == 0)
        {
            return (null, baseDiagnostics with
            {
                Status = CuratedSelectionForecastStatus.LimitedData,
                ActualPointCount = aggregateActualTimeSeries.Points.Count,
                ForecastPointCount = 0,
                Summary = "Forecast reference is unavailable for the current interval because historical windows are insufficient.",
                Signals = ["missing_forecast_reference"]
            });
        }

        var actualByTimestamp = aggregateActualTimeSeries.Points
            .GroupBy(point => point.TimestampUtc)
            .ToDictionary(group => group.Key, group => group.Last().Value);
        var forecastByTimestamp = aggregateForecastPoints
            .GroupBy(point => point.TimestampUtc)
            .ToDictionary(group => group.Key, group => group.Last().Value);

        var alignedTimestamps = actualByTimestamp.Keys
            .Intersect(forecastByTimestamp.Keys)
            .OrderBy(ts => ts)
            .ToList();

        var actualPointCount = aggregateActualTimeSeries.Points.Count;
        var forecastPointCount = aggregateForecastPoints.Count;
        var alignedPointCount = alignedTimestamps.Count;
        var alignmentCoverageRatio = actualPointCount > 0
            ? (double)alignedPointCount / actualPointCount
            : 0;

        if (alignedPointCount == 0)
        {
            return (null, baseDiagnostics with
            {
                Status = CuratedSelectionForecastStatus.LimitedData,
                Summary = "The actual and forecast series do not overlap in time after aggregation.",
                ActualPointCount = actualPointCount,
                ForecastPointCount = forecastPointCount,
                AlignedPointCount = 0,
                AlignmentCoverageRatio = alignmentCoverageRatio,
                Signals = ["no_aligned_points"]
            });
        }

        var absoluteErrorSum = 0d;
        var squaredErrorSum = 0d;
        var signedErrorSum = 0d;
        var absoluteActualSum = 0d;

        foreach (var timestamp in alignedTimestamps)
        {
            var actual = actualByTimestamp[timestamp];
            var forecast = forecastByTimestamp[timestamp];
            var error = actual - forecast;

            absoluteErrorSum += Math.Abs(error);
            squaredErrorSum += error * error;
            signedErrorSum += error;
            absoluteActualSum += Math.Abs(actual);
        }

        var maeKw = absoluteErrorSum / alignedPointCount;
        var rmseKw = Math.Sqrt(squaredErrorSum / alignedPointCount);
        var biasKw = signedErrorSum / alignedPointCount;
        var wapePercent = absoluteActualSum > 0.000001
            ? (absoluteErrorSum / absoluteActualSum) * 100.0
            : (double?)null;

        var averageAbsoluteActual = absoluteActualSum / alignedPointCount;
        var normalizedRmse = rmseKw / Math.Max(0.5, averageAbsoluteActual);
        var minimumAlignedSamples = Math.Max(6, (int)Math.Round(actualPointCount * 0.40));

        var isLimitedData = alignedPointCount < minimumAlignedSamples || alignmentCoverageRatio < 0.45;
        CuratedSelectionForecastStatus status;

        if (isLimitedData)
        {
            status = CuratedSelectionForecastStatus.LimitedData;
        }
        else
        {
            var wapeForStatus = wapePercent ?? (normalizedRmse * 100.0);
            status = wapeForStatus switch
            {
                <= 12.0 => CuratedSelectionForecastStatus.Stable,
                <= 25.0 => CuratedSelectionForecastStatus.Watch,
                _ => CuratedSelectionForecastStatus.PoorFit
            };
        }

        var summary = status switch
        {
            CuratedSelectionForecastStatus.Stable => "Forecast is tracking stably, with a low actual-vs-forecast gap.",
            CuratedSelectionForecastStatus.Watch => "Forecast is usable, but the actual-vs-forecast gap needs attention.",
            CuratedSelectionForecastStatus.PoorFit => "Forecast has a weak fit against the actual series in the current interval.",
            CuratedSelectionForecastStatus.LimitedData => "Forecast is only indicative because aligned coverage is low or too few aligned points are available.",
            _ => "Forecast is unavailable."
        };

        var signals = new List<string>();
        if (isLimitedData)
        {
            signals.Add("limited_alignment");
        }

        if (baseDiagnostics.ForecastMissingNodeCount > 0)
        {
            signals.Add("partial_forecast_coverage");
        }

        if (baseDiagnostics.HasMixedSignedSelection)
        {
            signals.Add("mixed_sign_selection");
        }

        if (status == CuratedSelectionForecastStatus.PoorFit)
        {
            signals.Add("high_forecast_error");
        }

        var diagnostics = baseDiagnostics with
        {
            Status = status,
            Summary = summary,
            ActualPointCount = actualPointCount,
            ForecastPointCount = forecastPointCount,
            AlignedPointCount = alignedPointCount,
            AlignmentCoverageRatio = alignmentCoverageRatio,
            MaeKw = maeKw,
            RmseKw = rmseKw,
            BiasKw = biasKw,
            WapePercent = wapePercent,
            Signals = signals
        };

        var compareExcludedMessages = new List<string>();
        if (baseDiagnostics.ForecastMissingNodeCount > 0)
        {
            compareExcludedMessages.Add($"Forecast reference was unavailable for {baseDiagnostics.ForecastMissingNodeCount}/{Math.Max(1, baseDiagnostics.IncludedNodeCount)} analytically included nodes.");
        }

        var compareData = new CuratedNodeCompareTimeSeriesResult
        {
            PrimaryNodeKey = "selection_set",
            Title = "Forecast vs Actual",
            Unit = aggregateActualTimeSeries.Unit,
            YAxisLabel = aggregateActualTimeSeries.YAxisLabel,
            Granularity = aggregateActualTimeSeries.Granularity,
            GranularityLabel = aggregateActualTimeSeries.GranularityLabel,
            AggregationMethod = "Forecast v1 uses transparent comparable windows from historical periods before the target interval. Actual and forecast are aggregated across the selection set without unsupported nodes.",
            InterpretationNote = "Forecast predicts expected behavior, while the baseline and deviation layer remains a separate reference mechanism for alerting.",
            RequestedMode = aggregateActualTimeSeries.RequestedMode,
            RequestedModeLabel = aggregateActualTimeSeries.RequestedModeLabel,
            Series =
            [
                new CuratedNodeCompareSeries
                {
                    NodeKey = "selection_set_actual",
                    Label = "Actual",
                    IsPrimary = true,
                    Points = aggregateActualTimeSeries.Points
                },
                new CuratedNodeCompareSeries
                {
                    NodeKey = "selection_set_forecast",
                    Label = "Forecast",
                    IsPrimary = false,
                    Points = aggregateForecastPoints
                }
            ],
            NoDataMessage = null
        };

        return (compareData, diagnostics);
    }

    private static CuratedSelectionOperationalHealthSummary BuildOperationalHealthSummary(
        CuratedSelectionCoverageSummary coverage,
        CuratedAggregateEnergyProfile energyProfile,
        IReadOnlyList<CuratedNodeDeviationSummary> deviationSummaries,
        CuratedNodeTimeSeriesResult? aggregateTimeSeries)
    {
        var selectedNodeCount = Math.Max(0, coverage.SelectedNodeCount);
        var supportedNodeCount = Math.Max(0, coverage.SupportedNodeCount);
        var includedNodeCount = Math.Max(0, coverage.IncludedNodeCount);
        var unsupportedNodeCount = Math.Max(0, coverage.UnsupportedNodeCount);
        var contextOnlyNodeCount = Math.Max(0, coverage.ContextOnlyNodeCount);
        var noDataNodeCount = Math.Max(0, coverage.NoDataNodeCount);

        var supportedSelectionRatio = selectedNodeCount > 0
            ? (double)supportedNodeCount / selectedNodeCount
            : 0;
        var includedCoverageRatio = supportedNodeCount > 0
            ? (double)includedNodeCount / supportedNodeCount
            : 0;

        var availableDeviations = deviationSummaries
            .Where(summary => summary.IsAvailable && summary.Severity.HasValue)
            .ToList();
        var highDeviationCount = availableDeviations.Count(summary => summary.Severity == NodeDeviationSeverity.High);
        var elevatedDeviationCount = availableDeviations.Count(summary => summary.Severity == NodeDeviationSeverity.Elevated);

        var hasWeakCoverage = supportedNodeCount > 0 && includedCoverageRatio < 0.75;
        var hasSevereCoverageGap = supportedNodeCount > 0 && includedCoverageRatio < 0.40;
        var hasNoSupportedNodes = supportedNodeCount == 0;
        var hasNoIncludedNodes = supportedNodeCount > 0 && includedNodeCount == 0;
        var hasMixedSignedSelection = energyProfile == CuratedAggregateEnergyProfile.MixedSigned;

        var abruptness = DetectAbruptAggregateShift(aggregateTimeSeries);
        var hasAbruptShift = abruptness.HasAbruptShift;

        var dataIssue = hasNoSupportedNodes
            || hasNoIncludedNodes
            || hasSevereCoverageGap
            || (noDataNodeCount > 0 && includedCoverageRatio < 0.60);

        var suspicious = !dataIssue && (
            highDeviationCount > 0
            || hasAbruptShift
            || (hasWeakCoverage && elevatedDeviationCount > 0)
            || (hasMixedSignedSelection && elevatedDeviationCount > 0));

        var attention = !dataIssue && !suspicious && (
            elevatedDeviationCount > 0
            || hasWeakCoverage
            || unsupportedNodeCount > 0
            || contextOnlyNodeCount > 0
            || noDataNodeCount > 0
            || hasMixedSignedSelection);

        var status = dataIssue
            ? CuratedSelectionAnomalyStatus.DataIssue
            : suspicious
                ? CuratedSelectionAnomalyStatus.Suspicious
                : attention
                    ? CuratedSelectionAnomalyStatus.Attention
                    : CuratedSelectionAnomalyStatus.Normal;

        var summary = status switch
        {
            CuratedSelectionAnomalyStatus.DataIssue when hasNoSupportedNodes => "The selection is outside the supported analytics set.",
            CuratedSelectionAnomalyStatus.DataIssue when hasNoIncludedNodes => "Supported nodes do not have usable data in the interval.",
            CuratedSelectionAnomalyStatus.DataIssue => "Data quality is low, so the analytics output has limited trustworthiness.",
            CuratedSelectionAnomalyStatus.Suspicious when highDeviationCount > 0 => "A strong deviation has been detected in part of the selection set.",
            CuratedSelectionAnomalyStatus.Suspicious when hasAbruptShift => "The aggregate series contains an abrupt shift relative to its normal level.",
            CuratedSelectionAnomalyStatus.Suspicious => "The combination of signals suggests suspicious behavior.",
            CuratedSelectionAnomalyStatus.Attention when elevatedDeviationCount > 0 => "The selection shows elevated deviation signals.",
            CuratedSelectionAnomalyStatus.Attention when hasWeakCoverage => "Coverage is weakened, so interpret the result cautiously.",
            CuratedSelectionAnomalyStatus.Attention when hasMixedSignedSelection => "The selection combines load and generation, so net interpretation requires extra care.",
            CuratedSelectionAnomalyStatus.Attention => "The selection needs additional attention.",
            _ => "No obvious anomaly is present; both deviation and coverage are stable."
        };

        var signals = new List<string>();
        if (unsupportedNodeCount > 0)
        {
            signals.Add("unsupported_nodes");
        }

        if (contextOnlyNodeCount > 0)
        {
            signals.Add("context_only_excluded");
        }

        if (noDataNodeCount > 0)
        {
            signals.Add("missing_interval_data");
        }

        if (hasWeakCoverage)
        {
            signals.Add("weak_coverage");
        }

        if (hasMixedSignedSelection)
        {
            signals.Add("mixed_sign_selection");
        }

        if (elevatedDeviationCount > 0)
        {
            signals.Add("elevated_deviation");
        }

        if (highDeviationCount > 0)
        {
            signals.Add("high_deviation");
        }

        if (hasAbruptShift)
        {
            signals.Add("abrupt_aggregate_shift");
        }

        return new CuratedSelectionOperationalHealthSummary
        {
            Status = status,
            Summary = summary,
            HighDeviationNodeCount = highDeviationCount,
            ElevatedDeviationNodeCount = elevatedDeviationCount,
            SelectedNodeCount = selectedNodeCount,
            SupportedNodeCount = supportedNodeCount,
            IncludedNodeCount = includedNodeCount,
            UnsupportedNodeCount = unsupportedNodeCount,
            ContextOnlyNodeCount = contextOnlyNodeCount,
            NoDataNodeCount = noDataNodeCount,
            HasWeakCoverage = hasWeakCoverage,
            HasMixedSignedSelection = hasMixedSignedSelection,
            HasAbruptAggregateShift = hasAbruptShift,
            SupportedSelectionRatio = supportedSelectionRatio,
            IncludedCoverageRatio = includedCoverageRatio,
            AbruptShiftRatio = abruptness.Ratio,
            Signals = signals
        };
    }

    private static CuratedSelectionLoadProfileSummary BuildLoadProfileSummary(
        CuratedNodeTimeSeriesResult? aggregateTimeSeries,
        DateTime from,
        DateTime to,
        CuratedAggregateEnergyProfile energyProfile,
        int includedNodeCount)
    {
        if (aggregateTimeSeries is null || aggregateTimeSeries.Points.Count < 4)
        {
            return new CuratedSelectionLoadProfileSummary
            {
                IsAvailable = false,
                Summary = "Load profile is unavailable because the aggregate time series does not contain enough points.",
                Methodology = "Daily profile v1 uses an hour-of-day average over the aggregate power series, with a transparent interval-snapshot fallback for short ranges.",
                DifferenceFromForecast = "Forecast estimates expected future behavior, while the load profile describes a typical historical pattern.",
                DifferenceFromMainChart = "The main chart keeps the exact time sequence, while the load profile highlights the repeated daily shape in an aggregated view."
            };
        }

        var orderedPoints = aggregateTimeSeries.Points
            .OrderBy(point => point.TimestampUtc)
            .ToList();

        var intervalDuration = to - from;
        var distinctDayCount = orderedPoints
            .Select(point => point.TimestampUtc.Date)
            .Distinct()
            .Count();

        var hasMixedSigns = energyProfile == CuratedAggregateEnergyProfile.MixedSigned;
        var selectionScope = includedNodeCount switch
        {
            <= 0 => "selection without included nodes",
            1 => "single-node selection",
            _ => $"aggregate of {includedNodeCount} included nodes"
        };

        var canUseDailyProfile = distinctDayCount >= 2 && intervalDuration >= TimeSpan.FromHours(36);
        if (canUseDailyProfile)
        {
            var hourlyBuckets = Enumerable.Range(0, 24)
                .Select(hour =>
                {
                    var hourPoints = orderedPoints
                        .Where(point => point.TimestampUtc.Hour == hour)
                        .ToList();

                    var sampleCount = hourPoints.Count;
                    var avg = sampleCount > 0
                        ? hourPoints.Average(point => point.Value)
                        : 0d;

                    return new CuratedSelectionLoadProfileBucket
                    {
                        BucketIndex = hour,
                        Label = $"{hour:00}:00-{((hour + 1) % 24):00}:00",
                        AverageKw = avg,
                        SampleCount = sampleCount
                    };
                })
                .Where(bucket => bucket.SampleCount > 0)
                .ToList();

            if (hourlyBuckets.Count == 0)
            {
                return new CuratedSelectionLoadProfileSummary
                {
                    IsAvailable = false,
                    Summary = "Load profile is unavailable because no valid points remain after hourly bucketing.",
                    Methodology = "Daily profile v1 uses an hour-of-day average over the aggregate power series, with a transparent interval-snapshot fallback for short ranges.",
                    DifferenceFromForecast = "Forecast estimates expected future behavior, while the load profile describes a typical historical pattern.",
                    DifferenceFromMainChart = "The main chart keeps the exact time sequence, while the load profile highlights the repeated daily shape in an aggregated view."
                };
            }

            var peakBucket = hourlyBuckets
                .OrderByDescending(bucket => Math.Abs(bucket.AverageKw))
                .First();

            var summary = hasMixedSigns
                ? $"Daily profile (hour-of-day) built from {distinctDayCount} days and {orderedPoints.Count} points ({selectionScope}). Mixed-sign semantics are preserved (+ load, - generation/export)."
                : $"Daily profile (hour-of-day) built from {distinctDayCount} days and {orderedPoints.Count} points ({selectionScope}). Most pronounced hourly bucket: {peakBucket.Label}.";

            return new CuratedSelectionLoadProfileSummary
            {
                IsAvailable = true,
                Mode = CuratedSelectionLoadProfileMode.HourOfDayAverage,
                ModeLabel = "Daily profile (hour-of-day)",
                IsFallback = false,
                PointCount = orderedPoints.Count,
                DistinctDayCount = distinctDayCount,
                HasMixedSigns = hasMixedSigns,
                Summary = summary,
                Methodology = "The profile is a transparent average of aggregate power (kW) by hour of day over the selected interval, with no black-box model.",
                DifferenceFromForecast = "The forecast layer estimates future behavior, while the load profile summarizes the typical repeating behavior within a day.",
                DifferenceFromMainChart = "The main chart preserves interval chronology, while the profile drops the specific date and keeps only the typical intraday pattern.",
                Buckets = hourlyBuckets
            };
        }

        const int fallbackBucketCount = 6;
        var referenceFrom = orderedPoints.First().TimestampUtc;
        var referenceToExclusive = orderedPoints.Last().TimestampUtc;
        var span = referenceToExclusive - referenceFrom;
        if (span <= TimeSpan.Zero)
        {
            span = TimeSpan.FromMinutes(1);
        }

        var fallbackStats = Enumerable.Range(0, fallbackBucketCount)
            .ToDictionary(index => index, _ => new RunningStats());

        foreach (var point in orderedPoints)
        {
            var progress = (point.TimestampUtc - referenceFrom).TotalMilliseconds / Math.Max(1d, span.TotalMilliseconds);
            var bucketIndex = (int)Math.Floor(progress * fallbackBucketCount);
            bucketIndex = Math.Clamp(bucketIndex, 0, fallbackBucketCount - 1);
            fallbackStats[bucketIndex].Add(point.Value);
        }

        var snapshotBuckets = Enumerable.Range(0, fallbackBucketCount)
            .Select(index =>
            {
                var bucketStart = referenceFrom.AddTicks((long)Math.Round(span.Ticks * (index / (double)fallbackBucketCount)));
                var bucketEnd = referenceFrom.AddTicks((long)Math.Round(span.Ticks * ((index + 1) / (double)fallbackBucketCount)));
                var stats = fallbackStats[index];

                return new CuratedSelectionLoadProfileBucket
                {
                    BucketIndex = index,
                    Label = $"S{index + 1} {bucketStart:HH:mm}-{bucketEnd:HH:mm}",
                    AverageKw = stats.Count > 0 ? stats.Sum / stats.Count : 0d,
                    SampleCount = stats.Count
                };
            })
            .Where(bucket => bucket.SampleCount > 0)
            .ToList();

        return new CuratedSelectionLoadProfileSummary
        {
            IsAvailable = snapshotBuckets.Count > 0,
            Mode = CuratedSelectionLoadProfileMode.IntervalSnapshotFallback,
            ModeLabel = "Interval snapshot fallback",
            IsFallback = true,
            PointCount = orderedPoints.Count,
            DistinctDayCount = distinctDayCount,
            HasMixedSigns = hasMixedSigns,
            Summary = $"The interval is short ({distinctDayCount} day(s)), so a {fallbackBucketCount}-segment snapshot is used instead of a typical daily profile.",
            Methodology = "The fallback profile uses the average aggregate power (kW) across evenly spaced segments of the current interval and does not claim to be a typical daily profile.",
            DifferenceFromForecast = "The forecast layer predicts what comes next, while the fallback profile is only a transparent structuring of the current short interval.",
            DifferenceFromMainChart = "The main chart shows every point, while the fallback profile compresses the interval into a few segments for faster reading of the operating shape.",
            Buckets = snapshotBuckets
        };
    }

    private static CuratedSelectionLoadDurationCurveSummary BuildLoadDurationCurveSummary(
        CuratedNodeTimeSeriesResult? aggregateTimeSeries,
        CuratedAggregateEnergyProfile energyProfile,
        TimeSpan requestedDuration)
    {
        if (aggregateTimeSeries is null)
        {
            return new CuratedSelectionLoadDurationCurveSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "No aggregate demand series is available for load-duration-curve evaluation.",
                EvaluationBasis = "Load duration curve requires an aggregate demand series.",
                Summary = "Load duration curve is unavailable because the aggregate demand series is missing.",
                Methodology = "Load duration curve sorts power demand values in descending order to show load persistence and distribution over the interval."
            };
        }

        var minimumPointCount = GetMinimumLoadDurationCurvePointCount(aggregateTimeSeries.Granularity);
        if (aggregateTimeSeries.Points.Count < minimumPointCount)
        {
            return new CuratedSelectionLoadDurationCurveSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "The interval is too short for a meaningful load-duration-curve.",
                EvaluationBasis = DescribePerformanceEvaluationBasis(aggregateTimeSeries),
                Summary = $"Load duration curve is unavailable because the aggregate demand series has only {aggregateTimeSeries.Points.Count} point(s); at least {minimumPointCount} are required for this evaluation.",
                Methodology = "Load duration curve sorts power demand values in descending order to show load persistence and distribution over the interval."
            };
        }

        if (requestedDuration < TimeSpan.FromHours(24))
        {
            return new CuratedSelectionLoadDurationCurveSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "The interval must be at least 24 hours for a meaningful load-duration-curve.",
                EvaluationBasis = DescribePerformanceEvaluationBasis(aggregateTimeSeries),
                Summary = "Load duration curve is unavailable because the interval is shorter than 24 hours.",
                Methodology = "Load duration curve sorts power demand values in descending order to show load persistence and distribution over the interval."
            };
        }

        if (!SupportsDemandPerformanceMetrics(energyProfile))
        {
            var unavailableSummary = energyProfile == CuratedAggregateEnergyProfile.GenerationOnly
                ? "Load duration curve is intentionally unavailable because the current selection is generation-only and this metric is demand-focused."
                : "Load duration curve is unavailable because the current selection does not expose a stable demand-positive aggregate.";

            return new CuratedSelectionLoadDurationCurveSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "Demand metrics are intentionally hidden for generation-only or non-demand selections.",
                EvaluationBasis = DescribePerformanceEvaluationBasis(aggregateTimeSeries),
                Summary = unavailableSummary,
                Methodology = "Load duration curve sorts power demand values in descending order. For mixed-sign selections, demand-positive projection is applied."
            };
        }

        var demandPoints = ProjectDemandSeries(aggregateTimeSeries)
            .OrderByDescending(point => point.Value)
            .ToList();

        if (demandPoints.Count == 0 || demandPoints.Max(p => p.Value) <= 0.0001d)
        {
            return new CuratedSelectionLoadDurationCurveSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "No measurable demand-positive periods remain after the demand-focused projection.",
                EvaluationBasis = DescribePerformanceEvaluationBasis(aggregateTimeSeries),
                HasMixedSigns = energyProfile == CuratedAggregateEnergyProfile.MixedSigned,
                PointCount = demandPoints.Count,
                Summary = "Load duration curve is unavailable because the interval does not contain measurable demand-positive periods.",
                Methodology = "Load duration curve sorts power demand values in descending order. For mixed-sign selections, demand-positive projection is applied."
            };
        }

        var peakDemand = demandPoints.First().Value;
        var averageDemand = demandPoints.Average(p => p.Value);
        var notes = new List<string>();
        var state = CuratedPerformanceKpiState.Available;
        var stateReason = "Load duration curve is computed directly from the aggregate demand series.";

        if (aggregateTimeSeries.Granularity != CuratedNodeTimeSeriesGranularity.Raw15Min)
        {
            state = CuratedPerformanceKpiState.Indicative;
            stateReason = "Load duration curve is evaluated on hourly-average demand for this interval.";
            notes.Add("Hourly-average evaluation smooths short spikes, so short-term peak persistence may not be fully visible.");
        }

        if (requestedDuration > HourlyTimeSeriesThreshold)
        {
            state = CuratedPerformanceKpiState.Indicative;
            notes.Add("This is a broad-interval duration summary; use it as directional context for load distribution rather than precision analysis.");
        }

        var curvePoints = new List<CuratedSelectionLoadDurationCurvePoint>();
        for (int i = 0; i < demandPoints.Count; i++)
        {
            var durationPercent = (i / (double)demandPoints.Count) * 100.0;
            curvePoints.Add(new CuratedSelectionLoadDurationCurvePoint
            {
                DurationPercent = durationPercent,
                DemandKw = demandPoints[i].Value
            });
        }

        var summary = energyProfile == CuratedAggregateEnergyProfile.MixedSigned
            ? $"Load duration curve uses demand-positive projection. Peak demand is {peakDemand.ToString("N2", CultureInfo.InvariantCulture)} kW; average is {averageDemand.ToString("N2", CultureInfo.InvariantCulture)} kW."
            : $"Peak demand is {peakDemand.ToString("N2", CultureInfo.InvariantCulture)} kW; average is {averageDemand.ToString("N2", CultureInfo.InvariantCulture)} kW.";

        if (energyProfile == CuratedAggregateEnergyProfile.MixedSigned)
        {
            stateReason = "Demand-focused view of a mixed-sign aggregate.";
            notes.Add("Negative net values are clipped to 0 kW before sorting, so export periods do not appear on this curve.");
        }

        return new CuratedSelectionLoadDurationCurveSummary
        {
            IsAvailable = true,
            State = state,
            StateReason = stateReason,
            EvaluationBasis = DescribePerformanceEvaluationBasis(aggregateTimeSeries),
            Notes = notes,
            HasMixedSigns = energyProfile == CuratedAggregateEnergyProfile.MixedSigned,
            PointCount = demandPoints.Count,
            PeakDemandKw = peakDemand,
            AverageDemandKw = averageDemand,
            Summary = summary,
            Methodology = $"Load duration curve sorts power demand values in descending order and normalizes across 0-100% duration. Each point represents a ranked period within the interval. For mixed-sign selections, negative values are clamped to 0 kW so the curve stays demand-focused. {DescribePerformanceEvaluationBasis(aggregateTimeSeries)}",
            Points = curvePoints
        };
    }

    private static CuratedSelectionPeakAnalysisSummary BuildPeakAnalysisSummary(
        CuratedNodeTimeSeriesResult? aggregateTimeSeries,
        CuratedAggregateEnergyProfile energyProfile,
        TimeSpan requestedDuration)
    {
        static CuratedSelectionPeakEvent EmptyPeak(string label)
        {
            return new CuratedSelectionPeakEvent
            {
                Label = label,
                IsAvailable = false
            };
        }

        static CuratedSelectionPeakEvent ToPeak(string label, CuratedNodeTimeSeriesPoint point)
        {
            return new CuratedSelectionPeakEvent
            {
                Label = label,
                IsAvailable = true,
                ValueKw = point.Value,
                MagnitudeKw = Math.Abs(point.Value),
                TimestampUtc = point.TimestampUtc
            };
        }

        if (aggregateTimeSeries is null || aggregateTimeSeries.Points.Count == 0)
        {
            return new CuratedSelectionPeakAnalysisSummary
            {
                IsAvailable = false,
                EnergyProfile = energyProfile,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "No aggregate power series is available for peak evaluation.",
                EvaluationBasis = "Peak evaluation requires an aggregate power series.",
                Summary = "Peak analysis is unavailable without an aggregate time series.",
                Methodology = "Peak analysis v1 evaluates peak demand, peak generation/export, and the peak net absolute event from the selection-set aggregate power series."
            };
        }

        var orderedPoints = aggregateTimeSeries.Points
            .OrderBy(point => point.TimestampUtc)
            .ToList();

        var demandCandidate = orderedPoints
            .Where(point => point.Value > 0)
            .OrderByDescending(point => point.Value)
            .FirstOrDefault();

        var generationCandidate = orderedPoints
            .Where(point => point.Value < 0)
            .OrderBy(point => point.Value)
            .FirstOrDefault();

        var netAbsoluteCandidate = orderedPoints
            .OrderByDescending(point => Math.Abs(point.Value))
            .First();

        var magnitudes = orderedPoints
            .Select(point => Math.Abs(point.Value))
            .Where(value => value > 0.0001)
            .ToList();

        var typicalMagnitude = magnitudes.Count > 0
            ? Median(magnitudes)
            : 0d;

        double? significanceRatio = null;
        CuratedPeakSignificanceLevel significanceLevel = CuratedPeakSignificanceLevel.Low;

        if (typicalMagnitude > 0)
        {
            significanceRatio = Math.Abs(netAbsoluteCandidate.Value) / Math.Max(0.25, typicalMagnitude);
            significanceLevel = significanceRatio.Value switch
            {
                >= 3.0 => CuratedPeakSignificanceLevel.High,
                >= 1.8 => CuratedPeakSignificanceLevel.Medium,
                _ => CuratedPeakSignificanceLevel.Low
            };
        }

        var hasMixedSigns = energyProfile == CuratedAggregateEnergyProfile.MixedSigned;
        var notes = new List<string>();
        var state = CuratedPerformanceKpiState.Available;
        var stateReason = "Peak events are evaluated directly from the aggregate demand/export series.";
        var evaluationBasis = DescribePerformanceEvaluationBasis(aggregateTimeSeries);

        if (aggregateTimeSeries.Granularity != CuratedNodeTimeSeriesGranularity.Raw15Min)
        {
            state = CuratedPerformanceKpiState.Indicative;
            stateReason = "Peak events are evaluated on smoothed aggregate power for this interval.";
            notes.Add("Hourly-average evaluation smooths short spikes, so the true 15-minute peak may be higher.");
        }

        if (requestedDuration > HourlyTimeSeriesThreshold)
        {
            state = CuratedPerformanceKpiState.Indicative;
            notes.Add("This is a broad-interval peak summary; use it as directional context rather than a point-in-time operating verdict.");
        }

        if (hasMixedSigns)
        {
            notes.Add("Demand peak, generation/export peak, and the net absolute event are tracked separately because the aggregate includes both positive and negative power.");
        }

        var demandPeak = demandCandidate is null
            ? EmptyPeak("Peak demand")
            : ToPeak("Peak demand", demandCandidate);
        var generationPeak = generationCandidate is null
            ? EmptyPeak("Peak generation/export")
            : ToPeak("Peak generation/export", generationCandidate);
        var netPeak = ToPeak("Peak net absolute event", netAbsoluteCandidate);

        var summary = energyProfile switch
        {
            CuratedAggregateEnergyProfile.ConsumptionOnly => "The selection is consumption-only, so the main event is the demand peak and its significance relative to typical load.",
            CuratedAggregateEnergyProfile.GenerationOnly => "The selection is generation-only, so the main event is the generation or export peak and its significance.",
            CuratedAggregateEnergyProfile.MixedSigned => "The selection is mixed-sign, so demand peak, generation peak, and net absolute peak are tracked separately.",
            _ => "Peak analysis is only indicative because the selection does not contain pronounced energy events."
        };

        return new CuratedSelectionPeakAnalysisSummary
        {
            IsAvailable = true,
            HasMixedSigns = hasMixedSigns,
            EnergyProfile = energyProfile,
            State = state,
            StateReason = stateReason,
            EvaluationBasis = evaluationBasis,
            Notes = notes,
            DemandPeak = demandPeak,
            GenerationPeak = generationPeak,
            NetAbsolutePeak = netPeak,
            TypicalMagnitudeKw = typicalMagnitude > 0 ? typicalMagnitude : null,
            SignificanceRatio = significanceRatio,
            SignificanceLevel = significanceLevel,
            Summary = summary,
            Methodology = $"Peak analysis is more than a min/max KPI: it maps peak events to exact timestamps and compares their magnitude with the typical |kW| level of the aggregate series using the median |kW|. {evaluationBasis}"
        };
    }

    private static CuratedSelectionLoadFactorSummary BuildLoadFactorSummary(
        CuratedNodeTimeSeriesResult? aggregateTimeSeries,
        CuratedAggregateEnergyProfile energyProfile,
        TimeSpan requestedDuration)
    {
        if (aggregateTimeSeries is null)
        {
            return new CuratedSelectionLoadFactorSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "No aggregate demand series is available for load-factor evaluation.",
                EvaluationBasis = "Load factor requires an aggregate demand series.",
                Summary = "Load factor is unavailable because the aggregate demand series is missing.",
                Methodology = "Load factor = average demand / peak demand over the selected interval."
            };
        }

        var minimumPointCount = GetMinimumLoadFactorPointCount(aggregateTimeSeries.Granularity);
        if (aggregateTimeSeries.Points.Count < minimumPointCount)
        {
            return new CuratedSelectionLoadFactorSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "The interval is too short for a stable load-factor estimate.",
                EvaluationBasis = DescribePerformanceEvaluationBasis(aggregateTimeSeries),
                Summary = $"Load factor is unavailable because the aggregate demand series has only {aggregateTimeSeries.Points.Count} point(s); at least {minimumPointCount} are required for this evaluation basis.",
                Methodology = "Load factor = average demand / peak demand over the selected interval."
            };
        }

        if (!SupportsDemandPerformanceMetrics(energyProfile))
        {
            var unavailableSummary = energyProfile == CuratedAggregateEnergyProfile.GenerationOnly
                ? "Load factor is intentionally unavailable because the current selection is generation-only and this KPI is demand-focused."
                : "Load factor is unavailable because the current selection does not expose a stable demand-positive aggregate.";

            return new CuratedSelectionLoadFactorSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "Demand KPI are intentionally hidden for generation-only or non-demand selections.",
                EvaluationBasis = DescribePerformanceEvaluationBasis(aggregateTimeSeries),
                Summary = unavailableSummary,
                Methodology = "For mixed-sign selections, negative net values are clamped to 0 kW before average demand and peak demand are calculated."
            };
        }

        var demandPoints = ProjectDemandSeries(aggregateTimeSeries)
            .OrderBy(point => point.TimestampUtc)
            .ToList();

        var peakDemand = demandPoints.Max(point => point.Value);
        if (peakDemand <= 0.0001d)
        {
            return new CuratedSelectionLoadFactorSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "No measurable demand-positive periods remain after the demand-focused projection.",
                EvaluationBasis = DescribePerformanceEvaluationBasis(aggregateTimeSeries),
                HasMixedSigns = energyProfile == CuratedAggregateEnergyProfile.MixedSigned,
                PointCount = demandPoints.Count,
                Summary = "Load factor is unavailable because the interval does not contain measurable demand-positive periods.",
                Methodology = "Load factor = average demand / peak demand over the selected interval."
            };
        }

        var averageDemand = demandPoints.Average(point => point.Value);
        var loadFactor = averageDemand / peakDemand;
        var notes = new List<string>();
        var state = CuratedPerformanceKpiState.Available;
        var stateReason = "Demand utilization is computed directly from the aggregate demand series.";

        if (aggregateTimeSeries.Granularity != CuratedNodeTimeSeriesGranularity.Raw15Min)
        {
            state = CuratedPerformanceKpiState.Indicative;
            stateReason = "Load factor is evaluated on hourly-average demand for this interval.";
            notes.Add("Hourly-average evaluation smooths short spikes, so the true short-interval peak may be higher than the evaluation peak.");
        }

        if (requestedDuration > HourlyTimeSeriesThreshold)
        {
            state = CuratedPerformanceKpiState.Indicative;
            notes.Add("This is a broad-interval utilization summary; use it as directional context rather than a point-in-time efficiency claim.");
        }

        var summary = energyProfile == CuratedAggregateEnergyProfile.MixedSigned
            ? $"Load factor uses a demand-positive projection of the mixed-sign aggregate. Average projected demand reaches {(loadFactor * 100.0).ToString("N1", CultureInfo.InvariantCulture)} % of the observed projected peak."
            : $"Average demand reaches {(loadFactor * 100.0).ToString("N1", CultureInfo.InvariantCulture)} % of the observed peak over the selected interval.";

        if (energyProfile == CuratedAggregateEnergyProfile.MixedSigned)
        {
            state = CuratedPerformanceKpiState.Indicative;
            stateReason = "Demand-focused view of a mixed-sign aggregate.";
            notes.Add("Negative net values are clipped to 0 kW before the average and peak are calculated, so export periods do not count toward this KPI.");
        }

        return new CuratedSelectionLoadFactorSummary
        {
            IsAvailable = true,
            State = state,
            StateReason = stateReason,
            EvaluationBasis = DescribePerformanceEvaluationBasis(aggregateTimeSeries),
            Notes = notes,
            HasMixedSigns = energyProfile == CuratedAggregateEnergyProfile.MixedSigned,
            AverageDemandKw = averageDemand,
            PeakDemandKw = peakDemand,
            LoadFactorRatio = loadFactor,
            PointCount = demandPoints.Count,
            Summary = summary,
            Methodology = $"Load factor = average demand / peak demand across the selected interval. For mixed-sign selections, negative net values are clamped to 0 kW so the metric stays demand-focused. {DescribePerformanceEvaluationBasis(aggregateTimeSeries)}"
        };
    }

    private static CuratedSelectionAfterHoursLoadSummary BuildAfterHoursLoadSummary(
        CuratedNodeTimeSeriesResult? aggregateTimeSeries,
        CuratedAggregateEnergyProfile energyProfile,
        TimeSpan requestedDuration)
    {
        if (aggregateTimeSeries is null)
        {
            return new CuratedSelectionAfterHoursLoadSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "No aggregate demand series is available for schedule-based evaluation.",
                EvaluationBasis = "After-hours evaluation requires an aggregate demand series.",
                Summary = "After-hours load is unavailable because the aggregate demand series is missing.",
                Methodology = "After-hours demand compares weekday active hours (07:00-19:00) with weekday night periods and weekends."
            };
        }

        var minimumSeriesPointCount = GetMinimumAfterHoursSeriesPointCount(aggregateTimeSeries.Granularity);
        if (aggregateTimeSeries.Points.Count < minimumSeriesPointCount)
        {
            return new CuratedSelectionAfterHoursLoadSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "The interval is too short for a stable schedule split.",
                EvaluationBasis = DescribePerformanceEvaluationBasis(aggregateTimeSeries),
                Summary = $"After-hours load is unavailable because the aggregate demand series has only {aggregateTimeSeries.Points.Count} point(s); at least {minimumSeriesPointCount} are required for this schedule split.",
                Methodology = "After-hours demand compares weekday active hours (07:00-19:00) with weekday night periods and weekends."
            };
        }

        if (!SupportsDemandPerformanceMetrics(energyProfile))
        {
            var unavailableSummary = energyProfile == CuratedAggregateEnergyProfile.GenerationOnly
                ? "After-hours, night, and weekend demand are intentionally unavailable because the current selection is generation-only and this KPI is demand-focused."
                : "After-hours, night, and weekend demand are unavailable because the current selection does not expose a stable demand-positive aggregate.";

            return new CuratedSelectionAfterHoursLoadSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "Demand KPI are intentionally hidden for generation-only or non-demand selections.",
                EvaluationBasis = DescribePerformanceEvaluationBasis(aggregateTimeSeries),
                Summary = unavailableSummary,
                Methodology = "For mixed-sign selections, negative net values are clamped to 0 kW before schedule-window averages are calculated."
            };
        }

        var demandPoints = ProjectDemandSeries(aggregateTimeSeries)
            .OrderBy(point => point.TimestampUtc)
            .ToList();

        static bool IsWeekend(DateTime timestampUtc) => timestampUtc.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        static bool IsActiveWeekday(DateTime timestampUtc) => !IsWeekend(timestampUtc) && timestampUtc.Hour >= 7 && timestampUtc.Hour < 19;
        static bool IsNightWeekday(DateTime timestampUtc) => !IsWeekend(timestampUtc) && !IsActiveWeekday(timestampUtc);

        var activeWeekdayValues = demandPoints
            .Where(point => IsActiveWeekday(point.TimestampUtc))
            .Select(point => point.Value)
            .ToList();
        var afterHoursValues = demandPoints
            .Where(point => !IsActiveWeekday(point.TimestampUtc))
            .Select(point => point.Value)
            .ToList();
        var nightValues = demandPoints
            .Where(point => IsNightWeekday(point.TimestampUtc))
            .Select(point => point.Value)
            .ToList();
        var weekendValues = demandPoints
            .Where(point => IsWeekend(point.TimestampUtc))
            .Select(point => point.Value)
            .ToList();

        var minimumBucketSamples = GetMinimumAfterHoursBucketSampleCount(aggregateTimeSeries.Granularity);
        if (activeWeekdayValues.Count < minimumBucketSamples || afterHoursValues.Count < minimumBucketSamples)
        {
            return new CuratedSelectionAfterHoursLoadSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "The fixed schedule windows do not contain enough demand samples.",
                EvaluationBasis = DescribePerformanceEvaluationBasis(aggregateTimeSeries),
                HasMixedSigns = energyProfile == CuratedAggregateEnergyProfile.MixedSigned,
                ActiveWeekdaySampleCount = activeWeekdayValues.Count,
                AfterHoursSampleCount = afterHoursValues.Count,
                NightSampleCount = nightValues.Count,
                WeekendSampleCount = weekendValues.Count,
                Summary = $"After-hours load needs enough weekday active-hour and off-hour samples to compare fixed schedule windows. The current interval has {activeWeekdayValues.Count} active and {afterHoursValues.Count} after-hours point(s); at least {minimumBucketSamples} are required in each bucket.",
                Methodology = "After-hours demand compares weekday active hours (07:00-19:00) with all other timestamps. Night load uses weekday points outside 07:00-19:00. Weekend load uses Saturday/Sunday points."
            };
        }

        var activeWeekdayAverage = activeWeekdayValues.Average();
        var afterHoursAverage = afterHoursValues.Average();
        var referenceDemand = Math.Max(0.15d, activeWeekdayAverage);
        var usesReferenceFloor = activeWeekdayAverage < 0.15d;
        var afterHoursRatio = afterHoursAverage / referenceDemand;
        var notes = new List<string>();
        var state = CuratedPerformanceKpiState.Indicative;
        var stateReason = "Schedule heuristic v1 with fixed weekday 07:00-19:00 active hours; do not read it as occupancy truth.";

        if (aggregateTimeSeries.Granularity != CuratedNodeTimeSeriesGranularity.Raw15Min)
        {
            stateReason = "Schedule heuristic evaluated on hourly-average demand for this interval.";
            notes.Add("Hourly-average evaluation smooths short schedule transitions and may understate sharp after-hours spikes.");
        }

        if (requestedDuration > HourlyTimeSeriesThreshold)
        {
            notes.Add("This is a broad-interval schedule summary; use it as directional context rather than a definitive operational verdict.");
        }

        if (usesReferenceFloor)
        {
            stateReason = "Schedule heuristic uses a minimum 0.15 kW active reference because active weekday demand is very low.";
            notes.Add("Ratios use a 0.15 kW reference floor when active weekday demand is very small, which prevents extreme percentages on near-zero baselines.");
        }

        double? nightAverage = null;
        double? nightRatio = null;
        var minimumSubratioSamples = GetMinimumAfterHoursSubratioSampleCount(aggregateTimeSeries.Granularity);
        if (nightValues.Count >= minimumSubratioSamples)
        {
            nightAverage = nightValues.Average();
            nightRatio = nightAverage.Value / referenceDemand;
        }
        else if (nightValues.Count > 0)
        {
            notes.Add($"Night ratio is hidden because only {nightValues.Count} night sample(s) are available; at least {minimumSubratioSamples} are required.");
        }

        double? weekendAverage = null;
        double? weekendRatio = null;
        if (weekendValues.Count >= minimumSubratioSamples)
        {
            weekendAverage = weekendValues.Average();
            weekendRatio = weekendAverage.Value / referenceDemand;
        }
        else if (weekendValues.Count > 0)
        {
            notes.Add($"Weekend ratio is hidden because only {weekendValues.Count} weekend sample(s) are available; at least {minimumSubratioSamples} are required.");
        }

        var summary = $"Schedule heuristic v1: average after-hours demand equals {(afterHoursRatio * 100.0).ToString("N0", CultureInfo.InvariantCulture)} % of the fixed active-weekday reference.";
        if (nightRatio.HasValue)
        {
            summary += $" Night load is {(nightRatio.Value * 100.0).ToString("N0", CultureInfo.InvariantCulture)} %.";
        }

        if (weekendRatio.HasValue)
        {
            summary += $" Weekend load is {(weekendRatio.Value * 100.0).ToString("N0", CultureInfo.InvariantCulture)} %.";
        }

        if (energyProfile == CuratedAggregateEnergyProfile.MixedSigned)
        {
            stateReason = "Schedule heuristic over the demand-positive portion of a mixed-sign aggregate.";
            summary += " Mixed-sign intervals are converted to demand-positive kW before averaging.";
            notes.Add("Negative net values are clipped to 0 kW before schedule-window averages are calculated, so export periods do not count toward this KPI.");
        }

        return new CuratedSelectionAfterHoursLoadSummary
        {
            IsAvailable = true,
            State = state,
            StateReason = stateReason,
            EvaluationBasis = DescribePerformanceEvaluationBasis(aggregateTimeSeries),
            Notes = notes,
            HasMixedSigns = energyProfile == CuratedAggregateEnergyProfile.MixedSigned,
            AverageActiveWeekdayDemandKw = activeWeekdayAverage,
            AverageAfterHoursDemandKw = afterHoursAverage,
            AfterHoursRatio = afterHoursRatio,
            AverageNightDemandKw = nightAverage,
            NightRatio = nightRatio,
            AverageWeekendDemandKw = weekendAverage,
            WeekendRatio = weekendRatio,
            UsesReferenceFloor = usesReferenceFloor,
            ActiveWeekdaySampleCount = activeWeekdayValues.Count,
            AfterHoursSampleCount = afterHoursValues.Count,
            NightSampleCount = nightValues.Count,
            WeekendSampleCount = weekendValues.Count,
            Summary = summary,
            Methodology = $"Schedule heuristic v1: active weekday demand = average demand-positive kW on weekdays between 07:00 and 19:00. After-hours demand = all remaining timestamps. Night load = weekday hours outside 07:00-19:00. Weekend load = Saturday/Sunday. {DescribePerformanceEvaluationBasis(aggregateTimeSeries)}"
        };
    }

    private static bool SupportsDemandPerformanceMetrics(CuratedAggregateEnergyProfile energyProfile)
    {
        return energyProfile is CuratedAggregateEnergyProfile.ConsumptionOnly or CuratedAggregateEnergyProfile.MixedSigned;
    }

    private static int GetMinimumLoadFactorPointCount(CuratedNodeTimeSeriesGranularity granularity)
    {
        return granularity switch
        {
            CuratedNodeTimeSeriesGranularity.Raw15Min => 8,
            CuratedNodeTimeSeriesGranularity.HourlyAverage => 4,
            _ => 4
        };
    }

    private static int GetMinimumAfterHoursSeriesPointCount(CuratedNodeTimeSeriesGranularity granularity)
    {
        return granularity switch
        {
            CuratedNodeTimeSeriesGranularity.Raw15Min => 16,
            CuratedNodeTimeSeriesGranularity.HourlyAverage => 8,
            _ => 8
        };
    }

    private static int GetMinimumAfterHoursBucketSampleCount(CuratedNodeTimeSeriesGranularity granularity)
    {
        return granularity switch
        {
            CuratedNodeTimeSeriesGranularity.Raw15Min => 16,
            CuratedNodeTimeSeriesGranularity.HourlyAverage => 8,
            _ => int.MaxValue
        };
    }

    private static int GetMinimumAfterHoursSubratioSampleCount(CuratedNodeTimeSeriesGranularity granularity)
    {
        return granularity switch
        {
            CuratedNodeTimeSeriesGranularity.Raw15Min => 8,
            CuratedNodeTimeSeriesGranularity.HourlyAverage => 4,
            _ => int.MaxValue
        };
    }

    private static int GetMinimumLoadDurationCurvePointCount(CuratedNodeTimeSeriesGranularity granularity)
    {
        return granularity switch
        {
            CuratedNodeTimeSeriesGranularity.Raw15Min => 24,
            CuratedNodeTimeSeriesGranularity.HourlyAverage => 8,
            _ => 8
        };
    }

    private static string DescribePerformanceEvaluationBasis(CuratedNodeTimeSeriesResult aggregateTimeSeries)
    {
        return aggregateTimeSeries.Granularity switch
        {
            CuratedNodeTimeSeriesGranularity.Raw15Min => "Evaluation basis: 15-minute aggregate power.",
            CuratedNodeTimeSeriesGranularity.HourlyAverage => "Evaluation basis: hourly-average aggregate power for interval stability.",
            CuratedNodeTimeSeriesGranularity.DailyAverage => "Evaluation basis: daily-average aggregate power. Use only as broad directional context.",
            _ => "Evaluation basis: aggregate power series."
        };
    }

    private static IReadOnlyList<CuratedNodeTimeSeriesPoint> ProjectDemandSeries(CuratedNodeTimeSeriesResult aggregateTimeSeries)
    {
        return aggregateTimeSeries.Points
            .Select(point => new CuratedNodeTimeSeriesPoint
            {
                TimestampUtc = point.TimestampUtc,
                Value = Math.Max(0d, point.Value)
            })
            .ToList();
    }

    private static CuratedSelectionOperatingRegimeSummary BuildOperatingRegimeSummary(
        CuratedNodeTimeSeriesResult? aggregateTimeSeries,
        CuratedAggregateEnergyProfile energyProfile)
    {
        if (aggregateTimeSeries is null || aggregateTimeSeries.Points.Count < 6)
        {
            return new CuratedSelectionOperatingRegimeSummary
            {
                IsAvailable = false,
                Summary = "Operating regime summary is unavailable because the aggregate series is too short.",
                Methodology = "Operating regime v1 uses transparent heuristics over aggregate power: baseload proxy, peak-to-average, variability, and a weekday/weekend signal."
            };
        }

        var orderedPoints = aggregateTimeSeries.Points
            .OrderBy(point => point.TimestampUtc)
            .ToList();

        var absValues = orderedPoints
            .Select(point => Math.Abs(point.Value))
            .ToList();

        var averageAbs = absValues.Average();
        var peakAbs = absValues.Max();
        var baseload = Percentile(absValues, 0.20);
        var variability = StandardDeviation(absValues) / Math.Max(0.25, averageAbs);
        var peakToAverage = peakAbs / Math.Max(0.25, averageAbs);

        var weekdayAbs = orderedPoints
            .Where(point => point.TimestampUtc.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            .Select(point => Math.Abs(point.Value))
            .ToList();
        var weekendAbs = orderedPoints
            .Where(point => point.TimestampUtc.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            .Select(point => Math.Abs(point.Value))
            .ToList();

        double? weekdayWeekendDeltaPercent = null;
        if (weekdayAbs.Count >= 4 && weekendAbs.Count >= 4)
        {
            var weekdayAvg = weekdayAbs.Average();
            var weekendAvg = weekendAbs.Average();
            weekdayWeekendDeltaPercent = ((weekdayAvg - weekendAvg) / Math.Max(0.25, averageAbs)) * 100.0;
        }

        var signals = new List<string>();
        if (peakToAverage >= 2.4)
        {
            signals.Add("high_peak_to_average");
        }

        if (variability >= 0.90)
        {
            signals.Add("high_variability");
        }
        else if (variability >= 0.60)
        {
            signals.Add("elevated_variability");
        }

        if (averageAbs > 0 && (baseload / Math.Max(0.25, averageAbs)) >= 0.75)
        {
            signals.Add("stable_baseload_shape");
        }

        if (weekdayWeekendDeltaPercent.HasValue && Math.Abs(weekdayWeekendDeltaPercent.Value) >= 20.0)
        {
            signals.Add("weekday_weekend_split");
        }

        if (energyProfile == CuratedAggregateEnergyProfile.MixedSigned)
        {
            signals.Add("mixed_sign_selection");
        }

        var summary = peakToAverage >= 2.4 || variability >= 0.90
            ? "The selection has a strongly peak-driven profile."
            : (baseload / Math.Max(0.25, averageAbs)) >= 0.75 && variability < 0.45
                ? "The selection has a stable baseload character."
                : variability >= 0.60
                    ? "The selection has pronounced daily variability."
                    : "The selection has a balanced operating regime without extreme peakiness.";

        if (weekdayWeekendDeltaPercent.HasValue && Math.Abs(weekdayWeekendDeltaPercent.Value) >= 20.0)
        {
            var direction = weekdayWeekendDeltaPercent.Value > 0 ? "weekday" : "weekend";
            summary += $" Strong weekday/weekend difference detected ({direction} dominant).";
        }

        if (energyProfile == CuratedAggregateEnergyProfile.MixedSigned)
        {
            summary += " The selection is mixed-sign, so the metrics are computed over |kW|.";
        }

        return new CuratedSelectionOperatingRegimeSummary
        {
            IsAvailable = true,
            HasMixedSigns = energyProfile == CuratedAggregateEnergyProfile.MixedSigned,
            BaseloadKw = baseload,
            AverageAbsoluteKw = averageAbs,
            PeakAbsoluteKw = peakAbs,
            PeakToAverageRatio = peakToAverage,
            VariabilityCoefficient = variability,
            WeekdayWeekendDeltaPercent = weekdayWeekendDeltaPercent,
            WeekdaySampleCount = weekdayAbs.Count,
            WeekendSampleCount = weekendAbs.Count,
            Summary = summary,
            Methodology = "Regime heuristic v1: baseload proxy = 20th percentile of |kW|, peak-to-average = max(|kW|)/avg(|kW|), variability = std(|kW|)/avg(|kW|), and the weekday/weekend signal = difference of averages normalized by avg(|kW|).",
            Signals = signals
        };
    }

    private static CuratedSelectionEmsEvaluationSummary BuildEmsEvaluationSummary(
        CuratedNodeTimeSeriesResult? aggregateTimeSeries,
        CuratedSelectionLoadProfileSummary loadProfile,
        CuratedSelectionPeakAnalysisSummary peakAnalysis,
        CuratedSelectionOperatingRegimeSummary operatingRegime,
        CuratedAggregateEnergyProfile energyProfile,
        double totalConsumptionKwh,
        double totalGenerationKwh,
        double netEnergyKwh)
    {
        var hasMixedSigns = energyProfile == CuratedAggregateEnergyProfile.MixedSigned;
        var hasConsumption = energyProfile is CuratedAggregateEnergyProfile.ConsumptionOnly or CuratedAggregateEnergyProfile.MixedSigned;
        var hasGeneration = energyProfile is CuratedAggregateEnergyProfile.GenerationOnly or CuratedAggregateEnergyProfile.MixedSigned;

        if (aggregateTimeSeries is null || aggregateTimeSeries.Points.Count < 6 || !operatingRegime.IsAvailable)
        {
            return new CuratedSelectionEmsEvaluationSummary
            {
                IsAvailable = false,
                HasMixedSigns = hasMixedSigns,
                HasConsumption = hasConsumption,
                HasGeneration = hasGeneration,
                Summary = "EMS evaluation v1 is unavailable because the aggregate series is too short or operating-regime metrics are missing.",
                Methodology = "EMS evaluation v1 uses transparent rules over the load-profile, peak-analysis, and operating-regime layers.",
                DistinctionNote = "Issue = data or coverage problem, anomaly = deviation from baseline, inefficiency = persistent operational inefficiency."
            };
        }

        var orderedPoints = aggregateTimeSeries.Points
            .OrderBy(point => point.TimestampUtc)
            .ToList();

        static CuratedOperationalScorecardStatus ClassifyHighIsWorse(double value, double watchThreshold, double issueThreshold)
        {
            if (value >= issueThreshold)
            {
                return CuratedOperationalScorecardStatus.Issue;
            }

            return value >= watchThreshold
                ? CuratedOperationalScorecardStatus.Watch
                : CuratedOperationalScorecardStatus.Good;
        }

        static CuratedOperationalScorecardStatus ClassifyLowIsWorse(double value, double watchThreshold, double issueThreshold)
        {
            if (value <= issueThreshold)
            {
                return CuratedOperationalScorecardStatus.Issue;
            }

            return value <= watchThreshold
                ? CuratedOperationalScorecardStatus.Watch
                : CuratedOperationalScorecardStatus.Good;
        }

        static (double? Ratio, int ActiveCount, int OffCount) ComputeOffHoursToActiveRatio(
            IReadOnlyList<CuratedNodeTimeSeriesPoint> points,
            Func<double, double> valueSelector)
        {
            var activeValues = points
                .Where(point => point.TimestampUtc.Hour >= 7 && point.TimestampUtc.Hour < 19)
                .Select(point => valueSelector(point.Value))
                .Where(value => value >= 0)
                .ToList();
            var offHoursValues = points
                .Where(point => point.TimestampUtc.Hour < 7 || point.TimestampUtc.Hour >= 19)
                .Select(point => valueSelector(point.Value))
                .Where(value => value >= 0)
                .ToList();

            if (activeValues.Count < 4 || offHoursValues.Count < 4)
            {
                return (null, activeValues.Count, offHoursValues.Count);
            }

            var activeAverage = activeValues.Average();
            var offAverage = offHoursValues.Average();
            var ratio = offAverage / Math.Max(0.15, activeAverage);
            return (ratio, activeValues.Count, offHoursValues.Count);
        }

        static (double? Ratio, int WeekdayCount, int WeekendCount) ComputeWeekendToWeekdayRatio(
            IReadOnlyList<CuratedNodeTimeSeriesPoint> points,
            Func<double, double> valueSelector)
        {
            var weekdayValues = points
                .Where(point => point.TimestampUtc.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                .Select(point => valueSelector(point.Value))
                .Where(value => value >= 0)
                .ToList();
            var weekendValues = points
                .Where(point => point.TimestampUtc.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                .Select(point => valueSelector(point.Value))
                .Where(value => value >= 0)
                .ToList();

            if (weekdayValues.Count < 4 || weekendValues.Count < 4)
            {
                return (null, weekdayValues.Count, weekendValues.Count);
            }

            var weekdayAverage = weekdayValues.Average();
            var weekendAverage = weekendValues.Average();
            var ratio = weekendAverage / Math.Max(0.15, weekdayAverage);
            return (ratio, weekdayValues.Count, weekendValues.Count);
        }

        var scorecards = new List<CuratedSelectionOperationalScorecard>();
        var offHoursRatio = (double?)null;
        var weekendRatio = (double?)null;
        var activeInactiveSeparation = (double?)null;
        var localGenerationUtilization = (double?)null;

        if (hasConsumption)
        {
            var normalizedConsumption = hasMixedSigns
                ? new Func<double, double>(value => Math.Max(0, value))
                : new Func<double, double>(value => Math.Abs(value));

            var (offToActiveRatio, activeCount, offCount) = ComputeOffHoursToActiveRatio(orderedPoints, normalizedConsumption);
            if (offToActiveRatio.HasValue)
            {
                offHoursRatio = offToActiveRatio.Value;
                activeInactiveSeparation = 1.0 - Math.Min(1.0, offHoursRatio.Value);
                var status = ClassifyHighIsWorse(offHoursRatio.Value, 0.45, 0.70);
                scorecards.Add(new CuratedSelectionOperationalScorecard
                {
                    Key = "off_hours_load",
                    Label = "Off-hours load indicator",
                    Status = status,
                    MetricValue = offHoursRatio,
                    MetricDisplay = (offHoursRatio.Value * 100.0).ToString("N0") + " %",
                    MetricLabel = "Off-hours / active-hours load",
                    Thresholds = "Watch >= 45 %, Issue >= 70 %",
                    Summary = status switch
                    {
                        CuratedOperationalScorecardStatus.Issue => "High load persists outside active hours.",
                        CuratedOperationalScorecardStatus.Watch => "Load is elevated outside active hours.",
                        _ => "Load drops clearly outside active hours."
                    },
                    Methodology = $"Average aggregate load outside 07:00-19:00 versus the average during active hours. Sample count active/off-hours: {activeCount}/{offCount}."
                });
            }
            else
            {
                scorecards.Add(new CuratedSelectionOperationalScorecard
                {
                    Key = "off_hours_load",
                    Label = "Off-hours load indicator",
                    Status = CuratedOperationalScorecardStatus.Unavailable,
                    Summary = "There are not enough points for a reliable off-hours load evaluation.",
                    Methodology = "At least 4 points are required in both active and off-hours windows."
                });
            }

            var (weekendToWeekdayRatio, weekdayCount, weekendCount) = ComputeWeekendToWeekdayRatio(orderedPoints, normalizedConsumption);
            if (weekendToWeekdayRatio.HasValue)
            {
                weekendRatio = weekendToWeekdayRatio.Value;
                var status = ClassifyHighIsWorse(weekendToWeekdayRatio.Value, 0.55, 0.80);
                scorecards.Add(new CuratedSelectionOperationalScorecard
                {
                    Key = "weekend_load",
                    Label = "Weekend load indicator",
                    Status = status,
                    MetricValue = weekendToWeekdayRatio,
                    MetricDisplay = (weekendToWeekdayRatio.Value * 100.0).ToString("N0") + " %",
                    MetricLabel = "Weekend / weekday load",
                    Thresholds = "Watch >= 55 %, Issue >= 80 %",
                    Summary = status switch
                    {
                        CuratedOperationalScorecardStatus.Issue => "Weekend load is approaching the weekday operating pattern.",
                        CuratedOperationalScorecardStatus.Watch => "Weekend load is elevated versus expectation.",
                        _ => "Weekend load is clearly separated from weekdays."
                    },
                    Methodology = $"Ratio of average weekend load to average weekday load. Sample count weekday/weekend: {weekdayCount}/{weekendCount}."
                });
            }
            else
            {
                scorecards.Add(new CuratedSelectionOperationalScorecard
                {
                    Key = "weekend_load",
                    Label = "Weekend load indicator",
                    Status = CuratedOperationalScorecardStatus.Unavailable,
                    Summary = "There is not enough weekday/weekend data for a stable weekend indicator.",
                    Methodology = "At least 4 points are required in both weekday and weekend samples."
                });
            }

            var baseloadRatio = operatingRegime.BaseloadKw.HasValue && operatingRegime.AverageAbsoluteKw.HasValue
                ? operatingRegime.BaseloadKw.Value / Math.Max(0.25, operatingRegime.AverageAbsoluteKw.Value)
                : (double?)null;

            if (baseloadRatio.HasValue)
            {
                var status = ClassifyHighIsWorse(baseloadRatio.Value, 0.50, 0.72);
                scorecards.Add(new CuratedSelectionOperationalScorecard
                {
                    Key = "baseload_intensity",
                    Label = "Baseload intensity indicator",
                    Status = status,
                    MetricValue = baseloadRatio,
                    MetricDisplay = (baseloadRatio.Value * 100.0).ToString("N0") + " %",
                    MetricLabel = "Baseload / average absolute load",
                    Thresholds = "Watch >= 50 %, Issue >= 72 %",
                    Summary = status switch
                    {
                        CuratedOperationalScorecardStatus.Issue => "Baseload forms a dominant share of the average load.",
                        CuratedOperationalScorecardStatus.Watch => "Baseload is elevated and limits daily flexibility.",
                        _ => "Baseload intensity is proportionate to the average load."
                    },
                    Methodology = "Ratio of the baseload proxy (20th percentile of |kW|) to avg(|kW|) from the operating-regime layer."
                });
            }
            else
            {
                scorecards.Add(new CuratedSelectionOperationalScorecard
                {
                    Key = "baseload_intensity",
                    Label = "Baseload intensity indicator",
                    Status = CuratedOperationalScorecardStatus.Unavailable,
                    Summary = "Baseload intensity cannot be evaluated without operating-regime metrics.",
                    Methodology = "It relies on the baseload proxy and avg(|kW|) metrics."
                });
            }
        }
        else
        {
            scorecards.Add(new CuratedSelectionOperationalScorecard
            {
                Key = "off_hours_load",
                Label = "Off-hours load indicator",
                Status = CuratedOperationalScorecardStatus.Unavailable,
                Summary = "The selection is generation-only, so the load indicator is not used.",
                Methodology = "V1 load indicators are intended for selections with non-zero consumption."
            });
            scorecards.Add(new CuratedSelectionOperationalScorecard
            {
                Key = "weekend_load",
                Label = "Weekend load indicator",
                Status = CuratedOperationalScorecardStatus.Unavailable,
                Summary = "The selection is generation-only, so the weekend load indicator is not used.",
                Methodology = "V1 load indicators are intended for selections with non-zero consumption."
            });
            scorecards.Add(new CuratedSelectionOperationalScorecard
            {
                Key = "baseload_intensity",
                Label = "Baseload intensity indicator",
                Status = CuratedOperationalScorecardStatus.Unavailable,
                Summary = "The selection is generation-only, so baseload intensity is not used.",
                Methodology = "V1 baseload intensity is intended primarily for load-dominant operation."
            });
        }

        var peakStressRatio = new[] { peakAnalysis.SignificanceRatio, operatingRegime.PeakToAverageRatio }
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .DefaultIfEmpty()
            .Max();

        if (peakStressRatio > 0)
        {
            var peakStatus = ClassifyHighIsWorse(peakStressRatio, 1.9, 2.7);
            scorecards.Add(new CuratedSelectionOperationalScorecard
            {
                Key = "peak_stress",
                Label = "Peak stress indicator",
                Status = peakStatus,
                MetricValue = peakStressRatio,
                MetricDisplay = peakStressRatio.ToString("N2") + "x",
                MetricLabel = "Max of peak significance and peak/avg ratio",
                Thresholds = "Watch >= 1.9x, Issue >= 2.7x",
                Summary = peakStatus switch
                {
                    CuratedOperationalScorecardStatus.Issue => "The regime is strongly peak-driven and demanding.",
                    CuratedOperationalScorecardStatus.Watch => "Peak stress is elevated and may increase operating costs.",
                    _ => "Peak stress remains within a normal operating band."
                },
                Methodology = "It transparently combines the peak-significance ratio (peak analysis) and the peak-to-average ratio (operating regime)."
            });
        }
        else
        {
            scorecards.Add(new CuratedSelectionOperationalScorecard
            {
                Key = "peak_stress",
                Label = "Peak stress indicator",
                Status = CuratedOperationalScorecardStatus.Unavailable,
                Summary = "Peak stress cannot be evaluated without peak or regime metrics.",
                Methodology = "It requires either peak significance or the peak-to-average ratio."
            });
        }

        if (hasGeneration && totalGenerationKwh > 0.0001)
        {
            var exportShare = netEnergyKwh < 0
                ? Math.Min(1.0, Math.Abs(netEnergyKwh) / Math.Max(0.1, totalGenerationKwh))
                : 0d;
            localGenerationUtilization = Math.Clamp(1.0 - exportShare, 0.0, 1.0);
            var generationStatus = ClassifyLowIsWorse(localGenerationUtilization.Value, 0.65, 0.40);
            scorecards.Add(new CuratedSelectionOperationalScorecard
            {
                Key = "local_generation_utilization",
                Label = "Local generation utilization indicator",
                Status = generationStatus,
                MetricValue = localGenerationUtilization,
                MetricDisplay = (localGenerationUtilization.Value * 100.0).ToString("N0") + " %",
                MetricLabel = "Estimated local utilization of generated energy",
                Thresholds = "Watch <= 65 %, Issue <= 40 %",
                Summary = generationStatus switch
                {
                    CuratedOperationalScorecardStatus.Issue => "The selection shows a strong export tendency and low local generation utilization.",
                    CuratedOperationalScorecardStatus.Watch => "Local generation utilization is limited and export appears more often.",
                    _ => "Generation is used mostly locally within the selection set."
                },
                Methodology = "Local utilization = 1 - export share, where export share is estimated from the net negative balance relative to total generation in the interval."
            });
        }
        else
        {
            scorecards.Add(new CuratedSelectionOperationalScorecard
            {
                Key = "local_generation_utilization",
                Label = "Local generation utilization indicator",
                Status = CuratedOperationalScorecardStatus.Unavailable,
                Summary = "The selection has no meaningful generation, so the generation-utilization indicator is not used.",
                Methodology = "It is active only for selections with a non-zero generation share."
            });
        }

        var inefficiencies = new List<CuratedSelectionInefficiencyItem>();

        var offHoursCard = scorecards.First(card => card.Key == "off_hours_load");
        if (offHoursCard.Status is CuratedOperationalScorecardStatus.Issue or CuratedOperationalScorecardStatus.Watch)
        {
            inefficiencies.Add(new CuratedSelectionInefficiencyItem
            {
                Key = "elevated_off_hours_load",
                Label = "Elevated off-hours load",
                Severity = offHoursCard.Status == CuratedOperationalScorecardStatus.Issue
                    ? CuratedSelectionInefficiencySeverity.Issue
                    : CuratedSelectionInefficiencySeverity.Watch,
                IsTriggered = true,
                Summary = "Load remains higher outside active hours than is desirable for an efficient schedule.",
                Evidence = offHoursCard.MetricDisplay,
                Methodology = "Derived directly from the off-hours load indicator."
            });
        }

        var weekendCard = scorecards.First(card => card.Key == "weekend_load");
        if (weekendCard.Status is CuratedOperationalScorecardStatus.Issue or CuratedOperationalScorecardStatus.Watch)
        {
            inefficiencies.Add(new CuratedSelectionInefficiencyItem
            {
                Key = "elevated_weekend_load",
                Label = "Elevated weekend load",
                Severity = weekendCard.Status == CuratedOperationalScorecardStatus.Issue
                    ? CuratedSelectionInefficiencySeverity.Issue
                    : CuratedSelectionInefficiencySeverity.Watch,
                IsTriggered = true,
                Summary = "The weekend regime is too similar to weekday operation.",
                Evidence = weekendCard.MetricDisplay,
                Methodology = "Derived directly from the weekend load indicator."
            });
        }

        var peakCard = scorecards.First(card => card.Key == "peak_stress");
        if (peakCard.Status is CuratedOperationalScorecardStatus.Issue or CuratedOperationalScorecardStatus.Watch)
        {
            inefficiencies.Add(new CuratedSelectionInefficiencyItem
            {
                Key = "excessive_peak_stress",
                Label = "Excessive peak stress",
                Severity = peakCard.Status == CuratedOperationalScorecardStatus.Issue
                    ? CuratedSelectionInefficiencySeverity.Issue
                    : CuratedSelectionInefficiencySeverity.Watch,
                IsTriggered = true,
                Summary = "The profile shows elevated peakiness relative to typical load.",
                Evidence = peakCard.MetricDisplay,
                Methodology = "Derived from the peak-significance and peak-to-average metrics."
            });
        }

        if (activeInactiveSeparation.HasValue && activeInactiveSeparation.Value < 0.25)
        {
            var severity = activeInactiveSeparation.Value < 0.12
                ? CuratedSelectionInefficiencySeverity.Issue
                : CuratedSelectionInefficiencySeverity.Watch;

            inefficiencies.Add(new CuratedSelectionInefficiencyItem
            {
                Key = "weak_active_inactive_separation",
                Label = "Weak active/inactive separation",
                Severity = severity,
                IsTriggered = true,
                Summary = "The separation between active and inactive hours is weak.",
                Evidence = (activeInactiveSeparation.Value * 100.0).ToString("N0") + " % separation",
                Methodology = "Separation = 1 - min(1, off-hours/active ratio) from aggregate load."
            });
        }

        var generationCard = scorecards.First(card => card.Key == "local_generation_utilization");
        if (generationCard.Status is CuratedOperationalScorecardStatus.Issue or CuratedOperationalScorecardStatus.Watch)
        {
            inefficiencies.Add(new CuratedSelectionInefficiencyItem
            {
                Key = "export_tendency",
                Label = "Strong export tendency",
                Severity = generationCard.Status == CuratedOperationalScorecardStatus.Issue
                    ? CuratedSelectionInefficiencySeverity.Issue
                    : CuratedSelectionInefficiencySeverity.Watch,
                IsTriggered = true,
                Summary = "Generation is not used locally enough and the selection has an export-oriented profile.",
                Evidence = generationCard.MetricDisplay,
                Methodology = "Derived from the local-generation-utilization indicator and the signed energy balance."
            });
        }

        var orderedInefficiencies = inefficiencies
            .OrderByDescending(item => item.Severity)
            .ThenBy(item => item.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var opportunities = new List<string>();
        foreach (var inefficiency in orderedInefficiencies)
        {
            var opportunity = inefficiency.Key switch
            {
                "elevated_off_hours_load" => "The selection shows elevated load outside working hours.",
                "elevated_weekend_load" => "The weekend regime is approaching weekday operation.",
                "excessive_peak_stress" => "The selection has a strongly peak-driven profile.",
                "weak_active_inactive_separation" => "The separation between active and inactive hours is weak.",
                "export_tendency" => "The selection has a strong export tendency.",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(opportunity))
            {
                continue;
            }

            if (!opportunities.Contains(opportunity, StringComparer.CurrentCulture))
            {
                opportunities.Add(opportunity);
            }

            if (opportunities.Count >= 3)
            {
                break;
            }
        }

        if (opportunities.Count == 0)
        {
            opportunities.Add("No strong schedule-inefficiency candidate is visible in the current interval.");
        }

        var issueCount = orderedInefficiencies.Count(item => item.Severity == CuratedSelectionInefficiencySeverity.Issue);
        var watchCount = orderedInefficiencies.Count(item => item.Severity == CuratedSelectionInefficiencySeverity.Watch);

        var summary = issueCount > 0
            ? $"EMS evaluation detects {issueCount} schedule-inefficiency issue(s) and {watchCount} watch signal(s)."
            : watchCount > 0
                ? $"EMS evaluation shows {watchCount} schedule-inefficiency watch signal(s) with no hard issue."
                : "EMS evaluation does not show strong schedule inefficiency in the current interval.";

        var methodology = "EMS evaluation v1 combines transparent scorecards over existing metrics: off-hours/active ratio, weekend/weekday ratio, baseload/avg ratio, peak-stress ratio, and local generation utilization. No black-box score and no optimization engine.";

        if (loadProfile.IsFallback)
        {
            methodology += " For short intervals it uses the load-profile fallback snapshot, so weekend and off-hours signals have lower robustness.";
        }

        return new CuratedSelectionEmsEvaluationSummary
        {
            IsAvailable = true,
            HasMixedSigns = hasMixedSigns,
            HasConsumption = hasConsumption,
            HasGeneration = hasGeneration,
            Summary = summary,
            Methodology = methodology,
            DistinctionNote = "Issue = data or coverage problem, anomaly = deviation from baseline, inefficiency = persistent operational inefficiency.",
            Scorecards = scorecards,
            Inefficiencies = orderedInefficiencies,
            Opportunities = opportunities
        };
    }

    private static (bool HasAbruptShift, double? Ratio) DetectAbruptAggregateShift(CuratedNodeTimeSeriesResult? aggregateTimeSeries)
    {
        if (aggregateTimeSeries is null || aggregateTimeSeries.Points.Count < 4)
        {
            return (false, null);
        }

        var points = aggregateTimeSeries.Points
            .OrderBy(point => point.TimestampUtc)
            .ToList();

        var nonTrivialAbsValues = points
            .Select(point => Math.Abs(point.Value))
            .Where(value => value > 0.0001)
            .ToList();

        if (nonTrivialAbsValues.Count < 3)
        {
            return (false, null);
        }

        var medianAbsValue = Median(nonTrivialAbsValues);
        var maxStep = 0d;

        for (var i = 1; i < points.Count; i++)
        {
            var step = Math.Abs(points[i].Value - points[i - 1].Value);
            if (step > maxStep)
            {
                maxStep = step;
            }
        }

        var ratio = maxStep / Math.Max(0.5, medianAbsValue);
        var hasAbruptShift = ratio >= 3.0 && maxStep >= 5.0;
        return (hasAbruptShift, ratio);
    }

    public async Task<DateTime?> GetCuratedNodeMaxTimestampUtcAsync(string nodeKey, CancellationToken ct = default)
    {
        var source = ResolveCuratedNodeSource(nodeKey);
        if (source is null)
        {
            return null;
        }

        var filePath = ResolveCuratedFilePath(source);
        if (filePath is null)
        {
            return null;
        }

        if (_maxTimestampCache.TryGetValue(filePath, out var cachedTimestamp))
        {
            return cachedTimestamp;
        }

        var maxTimestamp = await GetMaxTimestampUtcAsync(filePath, ct);
        if (maxTimestamp.HasValue)
        {
            _maxTimestampCache[filePath] = maxTimestamp.Value;
        }

        return maxTimestamp;
    }

    public async Task<DateTime?> GetCuratedNodeMaxTimestampUtcAsync(string nodeKey, FacilitySignalCode exactSignalCode, CancellationToken ct = default)
    {
        var domain = await GetCuratedNodeTimeDomainUtcAsync(nodeKey, exactSignalCode, ct);
        return domain.MaxUtc;
    }

    public async Task<(DateTime? MinUtc, DateTime? MaxUtc)> GetCuratedSelectionTimeDomainUtcAsync(
        IEnumerable<string> nodeKeys,
        CancellationToken ct = default)
    {
        if (nodeKeys is null)
        {
            return (null, null);
        }

        var distinctNodeKeys = nodeKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctNodeKeys.Count == 0)
        {
            return (null, null);
        }

        DateTime? minUtc = null;
        DateTime? maxUtc = null;
        var visitedFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var nodeKey in distinctNodeKeys)
        {
            var source = ResolveCuratedNodeSource(nodeKey);
            if (source is null || !source.IsPowerSignal)
            {
                continue;
            }

            var filePath = ResolveCuratedFilePath(source);
            if (filePath is null || !visitedFilePaths.Add(filePath))
            {
                continue;
            }

            (DateTime? MinUtc, DateTime? MaxUtc) domain;
            if (_timeDomainCache.TryGetValue(filePath, out var cachedDomain))
            {
                domain = (cachedDomain.MinUtc, cachedDomain.MaxUtc);
            }
            else
            {
                domain = await GetTimeDomainUtcAsync(filePath, ct);
                if (domain.MinUtc.HasValue && domain.MaxUtc.HasValue)
                {
                    _timeDomainCache[filePath] = (domain.MinUtc.Value, domain.MaxUtc.Value);
                }
            }

            if (!domain.MinUtc.HasValue || !domain.MaxUtc.HasValue)
            {
                continue;
            }

            if (!minUtc.HasValue || domain.MinUtc.Value < minUtc.Value)
            {
                minUtc = domain.MinUtc.Value;
            }

            if (!maxUtc.HasValue || domain.MaxUtc.Value > maxUtc.Value)
            {
                maxUtc = domain.MaxUtc.Value;
            }
        }

        return (minUtc, maxUtc);
    }

    public async Task<(DateTime? MinUtc, DateTime? MaxUtc)> GetCuratedSelectionTimeDomainUtcAsync(
        IEnumerable<string> nodeKeys,
        FacilitySignalCode exactSignalCode,
        CancellationToken ct = default)
    {
        var bindingContexts = ResolveSignalBindingContexts(nodeKeys, exactSignalCode);
        if (bindingContexts.Count == 0)
        {
            return (null, null);
        }

        return await GetTimeDomainUtcForSourcesAsync(bindingContexts.Select(context => context.Source), ct);
    }

    public async Task<CuratedNodeCompareTimeSeriesResult?> GetCuratedCompareTimeSeriesAsync(
        string primaryNodeKey,
        IEnumerable<string> compareNodeKeys,
        DateTime from,
        DateTime to,
        CuratedNodeTimeSeriesMode mode = CuratedNodeTimeSeriesMode.Auto,
        CancellationToken ct = default)
    {
        if (!SupportsComparePreview(primaryNodeKey))
        {
            return null;
        }

        compareNodeKeys ??= [];

        var primaryTimeSeries = await GetCuratedTimeSeriesAsync(primaryNodeKey, from, to, mode, includeBaselineOverlay: false, ct);
        if (primaryTimeSeries is null)
        {
            return null;
        }

        var orderedCompareNodeKeys = new List<string>();
        var excludedNodeMessages = new List<string>();
        var seenNodeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            primaryNodeKey
        };

        foreach (var rawNodeKey in compareNodeKeys)
        {
            if (string.IsNullOrWhiteSpace(rawNodeKey))
            {
                continue;
            }

            if (!seenNodeKeys.Add(rawNodeKey))
            {
                continue;
            }

            if (!SupportsComparePreview(rawNodeKey))
            {
                excludedNodeMessages.Add($"{rawNodeKey}: compare preview is unavailable because this node has no dataset-backed time-series binding.");
                continue;
            }

            orderedCompareNodeKeys.Add(rawNodeKey);
        }

        var series = new List<CuratedNodeCompareSeries>();

        if (primaryTimeSeries.Points.Count > 0)
        {
            series.Add(new CuratedNodeCompareSeries
            {
                NodeKey = primaryNodeKey,
                Label = ResolveCompareSeriesLabel(primaryNodeKey),
                IsPrimary = true,
                Points = primaryTimeSeries.Points
            });
        }
        else
        {
            excludedNodeMessages.Add($"{primaryNodeKey}: {primaryTimeSeries.NoDataMessage ?? "no data is available in the analysis window"}.");
        }

        if (orderedCompareNodeKeys.Count > 0)
        {
            var compareTasks = orderedCompareNodeKeys
                .Select(async nodeKey =>
                {
                    var result = await GetCuratedTimeSeriesAsync(nodeKey, from, to, mode, includeBaselineOverlay: false, ct);
                    return (NodeKey: nodeKey, Result: result);
                })
                .ToArray();

            await Task.WhenAll(compareTasks);

            foreach (var task in compareTasks)
            {
                var nodeKey = task.Result.NodeKey;
                var result = task.Result.Result;

                if (result is null)
                {
                    excludedNodeMessages.Add($"{nodeKey}: failed to prepare a time series.");
                    continue;
                }

                if (result.Points.Count == 0)
                {
                    excludedNodeMessages.Add($"{nodeKey}: {result.NoDataMessage ?? "no data is available in the analysis window"}.");
                    continue;
                }

                series.Add(new CuratedNodeCompareSeries
                {
                    NodeKey = nodeKey,
                    Label = ResolveCompareSeriesLabel(nodeKey),
                    IsPrimary = false,
                    Points = result.Points
                });
            }
        }

        var noDataMessage = series.Count == 0
            ? "Compare preview has no available time series for the selected nodes and interval."
            : null;

        return new CuratedNodeCompareTimeSeriesResult
        {
            PrimaryNodeKey = primaryNodeKey,
            Title = "Compare chart preview",
            Unit = primaryTimeSeries.Unit,
            YAxisLabel = primaryTimeSeries.YAxisLabel,
            Granularity = primaryTimeSeries.Granularity,
            GranularityLabel = primaryTimeSeries.GranularityLabel,
            AggregationMethod = primaryTimeSeries.AggregationMethod,
            InterpretationNote = ResolveCompareInterpretationNote(primaryTimeSeries.Granularity),
            RequestedMode = primaryTimeSeries.RequestedMode,
            RequestedModeLabel = primaryTimeSeries.RequestedModeLabel,
            Series = series,
            ExcludedNodeMessages = excludedNodeMessages,
            NoDataMessage = noDataMessage
        };
    }

    public async Task<(DateTime? MinUtc, DateTime? MaxUtc)> GetCuratedNodeTimeDomainUtcAsync(string nodeKey, CancellationToken ct = default)
    {
        var source = ResolveCuratedNodeSource(nodeKey);
        if (source is null)
        {
            return (null, null);
        }

        var filePath = ResolveCuratedFilePath(source);
        if (filePath is null)
        {
            return (null, null);
        }

        if (_timeDomainCache.TryGetValue(filePath, out var cachedDomain))
        {
            return (cachedDomain.MinUtc, cachedDomain.MaxUtc);
        }

        var domain = await GetTimeDomainUtcAsync(filePath, ct);
        if (domain.MinUtc.HasValue && domain.MaxUtc.HasValue)
        {
            _timeDomainCache[filePath] = (domain.MinUtc.Value, domain.MaxUtc.Value);
        }

        return domain;
    }

    public async Task<(DateTime? MinUtc, DateTime? MaxUtc)> GetCuratedNodeTimeDomainUtcAsync(string nodeKey, FacilitySignalCode exactSignalCode, CancellationToken ct = default)
    {
        if (exactSignalCode.IsEmpty)
        {
            return (null, null);
        }

        var source = ResolveCuratedNodeSource(nodeKey, exactSignalCode);
        if (source is null)
        {
            return (null, null);
        }

        var filePath = ResolveCuratedFilePath(source);
        if (filePath is null)
        {
            return (null, null);
        }

        if (_timeDomainCache.TryGetValue(filePath, out var cachedDomain))
        {
            return (cachedDomain.MinUtc, cachedDomain.MaxUtc);
        }

        var domain = await GetTimeDomainUtcAsync(filePath, ct);
        if (domain.MinUtc.HasValue && domain.MaxUtc.HasValue)
        {
            _timeDomainCache[filePath] = (domain.MinUtc.Value, domain.MaxUtc.Value);
        }

        return domain;
    }

    public async Task<CuratedNodeDeviationSummary> GetCuratedDeviationSummaryAsync(string nodeKey, DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (FacilityBuiltInNodeTypes.IsLegacyWeatherNodeKey(nodeKey))
        {
            return new CuratedNodeDeviationSummary
            {
                IsAvailable = false,
                Methodology = "The weather node is only an explanatory factor in this sprint, so baseline deviation is not evaluated here yet.",
                Message = "Baseline is not available for this node or interval yet.",
                Unit = "Â°C"
            };
        }

        var source = ResolveCuratedNodeSource(nodeKey);
        if (source is null || !source.SupportsDeviation)
        {
            return new CuratedNodeDeviationSummary
            {
                IsAvailable = false,
                Methodology = BaselineMethodology,
                Message = "Baseline is not available for this node or interval yet."
            };
        }

        var filePath = ResolveCuratedFilePath(source);
        if (filePath is null)
        {
            return new CuratedNodeDeviationSummary
            {
                IsAvailable = false,
                Methodology = BaselineMethodology,
                Message = "The local reduced source is missing."
            };
        }

        return await CalculateDeviationAsync(nodeKey, filePath, source, from, to, ct);
    }

    private sealed class CuratedNodeSource
    {
        public required string FileName { get; init; }
        /// <summary>SloĹľka v D:\DataSet\data\ pro binding-based zdroje. Null pro legacy flat CSV.</summary>
        public string? MeterFolder { get; init; }
        public string? SourceFilePath { get; init; }
        public required string ColumnName { get; init; }
        public required string Title { get; init; }
        public string? NodeTypeHint { get; init; }
        public required string Unit { get; init; }
        public string SummaryLabel { get; init; } = "Souhrn";
        public string StatsUnit { get; init; } = string.Empty;
        public string StatsLabel { get; init; } = "Hodnota";
        public FacilitySignalSeriesSemantics SeriesSemantics { get; init; } = FacilitySignalSeriesSemantics.SampleSeries;
        public bool IsPowerSignal { get; init; }
        public double PowerToKilowattFactor { get; init; } = 1.0;
        public bool SupportsDeviation { get; init; } = true;
        public bool UsesFixedCsvSeriesFormat { get; init; }
    }

    private sealed record SignalBindingContext(
        string NodeKey,
        FacilityDataBindingRegistry.BindingRecord Binding,
        CuratedNodeSource Source);

    private sealed class RunningStats
    {
        public double Sum { get; private set; }
        public int Count { get; private set; }

        public void Add(double value)
        {
            Sum += value;
            Count++;
        }
    }

    private enum BaselineReferenceKind
    {
        SamePeriodPreviousYear,
        RecentComparablePeriod
    }

    private sealed record BaselineCandidate(
        DateTime From,
        DateTime To,
        BaselineReferenceKind Kind,
        string Label);

    private sealed record BaselineReferenceAggregate(
        BaselineCandidate Candidate,
        double Sum);

    private sealed record BaselineSelection(
        double Value,
        string StrategyDescription,
        IReadOnlyList<BaselineCandidate> ReferenceCandidates);

    private sealed record WeatherAwareDailyEnergySeries(
        IReadOnlyDictionary<DateTime, double> DailyEnergyByDay,
        IReadOnlyList<string> ContributingNodeKeys,
        int MatchingNodeCount,
        string Unit,
        string EvaluationBasis);

    private sealed record WeatherAwareDailyTemperatureSeries(
        IReadOnlyDictionary<DateTime, double> DailyAverageTemperatureByDay,
        string WeatherNodeKey,
        string EvaluationBasis);

    private sealed record DailyWeatherAwareObservation(
        DateTime DayStartUtc,
        double Energy,
        double AverageOutdoorTemperatureC);

    private sealed record WeatherAwareBaselineModel(
        double Intercept,
        double HeatingSlope,
        double CoolingSlope)
    {
        public double Predict(double averageOutdoorTemperatureC)
        {
            var heating = Math.Max(0, WeatherAwareHeatingBalanceTemperatureC - averageOutdoorTemperatureC);
            var cooling = Math.Max(0, averageOutdoorTemperatureC - WeatherAwareCoolingBalanceTemperatureC);
            return Math.Max(0, Intercept + (HeatingSlope * heating) + (CoolingSlope * cooling));
        }
    }

    private sealed record HourlyLoadScatterSeries(
        IReadOnlyDictionary<DateTime, double> LoadByHour,
        IReadOnlyList<string> ContributingNodeKeys,
        int MatchingNodeCount,
        string Unit,
        string YAxisLabel,
        string LoadBasisLabel,
        string EvaluationBasis,
        bool UsesEnergyDerivedLoad);

    private sealed record HourlyTemperatureScatterSeries(
        IReadOnlyDictionary<DateTime, double> TemperatureByHour,
        string WeatherNodeKey,
        string EvaluationBasis);

    private sealed record TimeSeriesGranularityDecision(
        CuratedNodeTimeSeriesGranularity Granularity,
        string Label,
        string AggregationMethod,
        CuratedNodeTimeSeriesMode RequestedMode,
        string RequestedModeLabel);

    /// <summary>
    /// VrĂˇtĂ­ CuratedNodeSource pro danĂ˝ nodeKey.
    /// PrimĂˇrnÄ›: vyhledĂˇ v FacilityDataBindingRegistry (novĂ˝ dataset).
    /// Fallback: legacy hardcoded mapping pro starĂ© uzly (pv_main, chp_main, ...).
    /// </summary>
    private CuratedNodeSource? ResolveCuratedNodeSource(string nodeKey)
        => ResolveCuratedNodeSource(nodeKey, null);

    private CuratedNodeSource? ResolveCuratedNodeSource(string nodeKey, FacilitySignalCode? exactSignalCode)
    {
        // â”€â”€ 1. Binding-based lookup (novĂ˝ dataset) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var binding = exactSignalCode.HasValue && !exactSignalCode.Value.IsEmpty
            ? _bindingRegistry.GetPreferredBinding(nodeKey, exactSignalCode.Value)
            : _bindingRegistry.GetPrimaryBinding(nodeKey);

        if (binding is not null)
        {
            return BuildBindingCuratedNodeSource(nodeKey, binding);
        }

        // â”€â”€ 2. Legacy fallback (starĂ© agregovanĂ© CSV uzly â€” zachovĂˇny pro pĹ™echod) â”€
        if (FacilityBuiltInNodeTypes.IsLegacyWeatherNodeKey(nodeKey))
        {
            return new CuratedNodeSource
            {
                FileName = "weather.csv",
                ColumnName = "WeatherStation.Weather.Ta",
                Title = "Instant average temperature",
                NodeTypeHint = FacilityBuiltInNodeTypes.WeatherNodeType,
                Unit = "Â°C",
                SummaryLabel = "Average temperature",
                StatsUnit = "Â°C",
                StatsLabel = "Temperature",
                SupportsDeviation = false
            };
        }

        return nodeKey switch
        {
            "pv_main" => new CuratedNodeSource
            {
                FileName = "electricity_P.csv",
                ColumnName = "PV",
                Title = "Solar generation (PV)",
                NodeTypeHint = "generator_pv",
                Unit = "kWh",
                SummaryLabel = "Interval energy",
                StatsUnit = "kW",
                StatsLabel = "Power",
                IsPowerSignal = true,
                PowerToKilowattFactor = 0.001,
                SupportsDeviation = true
            },
            "chp_main" => new CuratedNodeSource
            {
                FileName = "electricity_P.csv",
                ColumnName = "CHP",
                Title = "Cogeneration output (CHP)",
                NodeTypeHint = "generator_chp",
                Unit = "kWh",
                SummaryLabel = "Interval energy",
                StatsUnit = "kW",
                StatsLabel = "Power",
                IsPowerSignal = true,
                PowerToKilowattFactor = 0.001,
                SupportsDeviation = true
            },
            "cooling_main" => new CuratedNodeSource
            {
                FileName = "cooling_P.csv",
                ColumnName = "total",
                Title = "Total cooling",
                NodeTypeHint = "utility_cooling",
                Unit = "kWh",
                SummaryLabel = "Interval energy",
                StatsUnit = "kW",
                StatsLabel = "Power",
                IsPowerSignal = true,
                PowerToKilowattFactor = 0.001,
                SupportsDeviation = true
            },
            "heating_main" => new CuratedNodeSource
            {
                FileName = "heating_P.csv",
                ColumnName = "total",
                Title = "Total heating",
                NodeTypeHint = "utility_heating",
                Unit = "kWh",
                SummaryLabel = "Interval energy",
                StatsUnit = "kW",
                StatsLabel = "Power",
                IsPowerSignal = true,
                PowerToKilowattFactor = 0.001,
                SupportsDeviation = true
            },
            _ => null
        };
    }

    private static bool IsAdditiveSignalFamily(FacilitySignalFamily signalFamily)
        => signalFamily is FacilitySignalFamily.Power or FacilitySignalFamily.Energy;

    private CuratedNodeSource BuildBindingCuratedNodeSource(string nodeKey, FacilityDataBindingRegistry.BindingRecord binding)
    {
        var signalFamily = binding.SignalFamily;
        var exactSignalCode = binding.ExactSignalCode;
        var seriesSemantics = FacilitySignalTaxonomy.ResolveSeriesSemantics(exactSignalCode);
        var isPowerSignal = signalFamily == FacilitySignalFamily.Power;
        var isFlowSignal = exactSignalCode.Value is "qv" or "V";
        var isFixedCsvSeries = binding.UsesFixedCsvSeriesFormat;

        string unit;
        string summaryLabel;
        string statsUnit;
        string statsLabel;
        var powerToKilowattFactor = 1.0;

        if (signalFamily == FacilitySignalFamily.Power)
        {
            unit = "kWh";
            summaryLabel = "Interval energy";
            statsUnit = "kW";
            statsLabel = "Power";
            powerToKilowattFactor = ResolvePowerToKilowattFactor(ResolveImplicitPowerUnit(binding));
        }
        else if (signalFamily == FacilitySignalFamily.Energy)
        {
            unit = string.IsNullOrWhiteSpace(binding.Unit) ? "kWh" : binding.Unit;
            summaryLabel = "Interval energy";
            statsUnit = unit;
            statsLabel = seriesSemantics == FacilitySignalSeriesSemantics.CumulativeCounter
                ? "Interval energy"
                : "Energy";
        }
        else if (signalFamily == FacilitySignalFamily.WeatherTemperature)
        {
            unit = string.IsNullOrWhiteSpace(binding.Unit) ? "Â°C" : binding.Unit;
            summaryLabel = "Average temperature";
            statsUnit = unit;
            statsLabel = "Temperature";
        }
        else if (signalFamily == FacilitySignalFamily.Voltage)
        {
            unit = string.IsNullOrWhiteSpace(binding.Unit) ? "V" : binding.Unit;
            summaryLabel = "Average voltage";
            statsUnit = unit;
            statsLabel = "Voltage";
        }
        else if (signalFamily == FacilitySignalFamily.Current)
        {
            unit = string.IsNullOrWhiteSpace(binding.Unit) ? "A" : binding.Unit;
            summaryLabel = "Average current";
            statsUnit = unit;
            statsLabel = "Current";
        }
        else if (signalFamily == FacilitySignalFamily.PowerFactor)
        {
            unit = string.IsNullOrWhiteSpace(binding.Unit) ? string.Empty : binding.Unit;
            summaryLabel = "Average power factor";
            statsUnit = unit;
            statsLabel = "Power factor";
        }
        else if (signalFamily == FacilitySignalFamily.ReactivePower)
        {
            unit = string.IsNullOrWhiteSpace(binding.Unit) ? "kVAr" : binding.Unit;
            summaryLabel = "Reactive power";
            statsUnit = unit;
            statsLabel = "Reactive power";
        }
        else if (isFlowSignal)
        {
            unit = "mÂł";
            summaryLabel = "Volume";
            statsUnit = "mÂł";
            statsLabel = "Flow";
        }
        else
        {
            unit = !string.IsNullOrWhiteSpace(binding.Unit) ? binding.Unit : binding.Category;
            summaryLabel = "Value";
            statsUnit = string.Empty;
            statsLabel = "Value";
        }

        var title = !string.IsNullOrWhiteSpace(binding.MeterUrn)
            ? binding.MeterUrn
            : !string.IsNullOrWhiteSpace(binding.SourceLabel)
                ? binding.SourceLabel
                : nodeKey;
        var columnName = isFixedCsvSeries
            ? "value"
            : $"{binding.MeterUrn}.{binding.MeasurementKey}";

        return new CuratedNodeSource
        {
            FileName = binding.FileName,
            MeterFolder = binding.MeterFolder,
            SourceFilePath = binding.SourceFilePath,
            ColumnName = columnName,
            Title = title,
            NodeTypeHint = !string.IsNullOrWhiteSpace(binding.Category) ? binding.Category : signalFamily.ToString(),
            Unit = unit,
            SummaryLabel = summaryLabel,
            StatsUnit = statsUnit,
            StatsLabel = statsLabel,
            SeriesSemantics = seriesSemantics,
            IsPowerSignal = isPowerSignal,
            PowerToKilowattFactor = isPowerSignal ? powerToKilowattFactor : 1.0,
            SupportsDeviation = isPowerSignal,
            UsesFixedCsvSeriesFormat = isFixedCsvSeries,
        };
    }

    private List<SignalBindingContext> ResolveSignalBindingContexts(IEnumerable<string> nodeKeys, FacilitySignalCode exactSignalCode)
    {
        if (exactSignalCode.IsEmpty || nodeKeys is null)
        {
            return [];
        }

        var contexts = new List<SignalBindingContext>();

        foreach (var nodeKey in nodeKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var binding = _bindingRegistry.GetPreferredBinding(nodeKey, exactSignalCode);
            var source = ResolveCuratedNodeSource(nodeKey, exactSignalCode);
            if (binding is null || source is null)
            {
                continue;
            }

            contexts.Add(new SignalBindingContext(nodeKey, binding, source));
        }

        return contexts;
    }

    private SelectionSignalAvailabilityItem BuildSelectionSignalAvailabilityItem(
        IGrouping<string, SignalBindingContext> group,
        int scopeNodeCount)
    {
        var contextList = group.ToList();
        var firstContext = contextList[0];
        var matchingNodeKeys = contextList
            .Select(context => context.NodeKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(nodeKey => nodeKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var distinctUnits = contextList
            .Select(context => ResolveTimeSeriesUnit(context.Source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var signalFamily = firstContext.Binding.SignalFamily;
        var nonMatchingNodeCount = Math.Max(0, scopeNodeCount - matchingNodeKeys.Count);

        SelectionSignalAvailabilityKind availabilityKind;
        string availabilityLabel;
        string availabilityMessage;

        if (matchingNodeKeys.Count == 1)
        {
            availabilityKind = SelectionSignalAvailabilityKind.SingleNodeSeries;
            availabilityLabel = "Single-node series";
            availabilityMessage = scopeNodeCount == 1
                ? "Available on the current selection."
                : $"Available on 1 of {scopeNodeCount} nodes in the current scope.";
        }
        else if (!IsAdditiveSignalFamily(signalFamily))
        {
            availabilityKind = SelectionSignalAvailabilityKind.AggregateUnavailable;
            availabilityLabel = "Aggregate unsupported";
            availabilityMessage = $"{signalFamily} signals are not aggregated across multiple nodes in this step.";
        }
        else if (distinctUnits.Count > 1)
        {
            availabilityKind = SelectionSignalAvailabilityKind.AggregateUnavailable;
            availabilityLabel = "Incompatible aggregate";
            availabilityMessage = "Matching nodes expose incompatible units, so aggregate trend is unavailable.";
        }
        else
        {
            availabilityKind = SelectionSignalAvailabilityKind.AggregateSeries;
            availabilityLabel = "Aggregate sum";
            availabilityMessage = nonMatchingNodeCount > 0
                ? $"Aggregates {matchingNodeKeys.Count} matching nodes. {nonMatchingNodeCount} scope nodes do not expose this exact signal."
                : $"Aggregates all {matchingNodeKeys.Count} matching nodes in the current scope.";
        }

        return new SelectionSignalAvailabilityItem
        {
            ExactSignalCode = firstContext.Binding.ExactSignalCode,
            SignalFamily = signalFamily,
            Unit = distinctUnits.FirstOrDefault() ?? string.Empty,
            ScopeNodeCount = scopeNodeCount,
            MatchingNodeCount = matchingNodeKeys.Count,
            NonMatchingNodeCount = nonMatchingNodeCount,
            AvailabilityKind = availabilityKind,
            AvailabilityLabel = availabilityLabel,
            AvailabilityMessage = availabilityMessage,
            MatchingNodeKeys = matchingNodeKeys
        };
    }

    private async Task<CuratedNodeTimeSeriesResult?> GetSelectionSignalAggregateTimeSeriesAsync(
        SelectionSignalAvailabilityItem option,
        DateTime from,
        DateTime to,
        CuratedNodeTimeSeriesMode mode,
        CancellationToken ct)
    {
        var bindingContexts = ResolveSignalBindingContexts(option.MatchingNodeKeys, option.ExactSignalCode);
        if (bindingContexts.Count == 0)
        {
            return null;
        }

        var nodeTasks = bindingContexts
            .Select(context => GetCuratedTimeSeriesAsync(
                context.NodeKey,
                option.ExactSignalCode,
                from,
                to,
                mode,
                includeBaselineOverlay: false,
                ct))
            .ToArray();

        await Task.WhenAll(nodeTasks);

        var resolvedSeries = nodeTasks
            .Select(task => task.Result)
            .Where(result => result is not null)
            .Cast<CuratedNodeTimeSeriesResult>()
            .ToList();

        if (resolvedSeries.Count == 0)
        {
            return null;
        }

        var template = resolvedSeries.First();
        var seriesWithPoints = resolvedSeries
            .Where(result => result.Points.Count > 0)
            .ToList();

        if (seriesWithPoints.Count == 0)
        {
            return new CuratedNodeTimeSeriesResult
            {
                NodeKey = "selection_set",
                Title = $"{option.ExactSignalCode.Value} selection aggregate",
                Unit = template.Unit,
                YAxisLabel = template.YAxisLabel,
                SeriesSemantics = template.SeriesSemantics,
                UsesDerivedIntervalSeries = template.UsesDerivedIntervalSeries,
                SeriesStatusMessage = template.SeriesStatusMessage,
                Granularity = template.Granularity,
                GranularityLabel = template.GranularityLabel,
                AggregationMethod = $"{template.AggregationMethod} Selection aggregate: sum of matching {option.ExactSignalCode.Value} series at each timestamp.",
                InterpretationNote = ResolveSelectionSignalAggregateInterpretationNote(option.SignalFamily, template.SeriesSemantics, template.Granularity),
                RequestedMode = template.RequestedMode,
                RequestedModeLabel = template.RequestedModeLabel,
                BaselineOverlayRequested = false,
                BaselineOverlayAvailable = false,
                NoDataMessage = "No matching node has data for the selected signal in the current interval."
            };
        }

        return new CuratedNodeTimeSeriesResult
        {
            NodeKey = "selection_set",
            Title = $"{option.ExactSignalCode.Value} selection aggregate",
            Unit = template.Unit,
            YAxisLabel = template.YAxisLabel,
            SeriesSemantics = template.SeriesSemantics,
            UsesDerivedIntervalSeries = template.UsesDerivedIntervalSeries,
            SeriesStatusMessage = template.SeriesStatusMessage,
            Granularity = template.Granularity,
            GranularityLabel = template.GranularityLabel,
            AggregationMethod = $"{template.AggregationMethod} Selection aggregate: sum of matching {option.ExactSignalCode.Value} series at each timestamp.",
            InterpretationNote = ResolveSelectionSignalAggregateInterpretationNote(option.SignalFamily, template.SeriesSemantics, template.Granularity),
            RequestedMode = template.RequestedMode,
            RequestedModeLabel = template.RequestedModeLabel,
            BaselineOverlayRequested = false,
            BaselineOverlayAvailable = false,
            Points = SumTimeSeriesByTimestamp(seriesWithPoints.Select(result => result.Points))
        };
    }

    private static SelectionSignalBasicStats? BuildSelectionSignalBasicStats(CuratedNodeTimeSeriesResult? timeSeries)
    {
        if (timeSeries is null || timeSeries.Points.Count == 0)
        {
            return null;
        }

        var values = timeSeries.Points.Select(point => point.Value).ToList();
        return new SelectionSignalBasicStats
        {
            Min = values.Min(),
            Max = values.Max(),
            Average = values.Average(),
            PointCount = values.Count,
            FirstTimestampUtc = timeSeries.Points[0].TimestampUtc,
            LastTimestampUtc = timeSeries.Points[^1].TimestampUtc,
            Unit = timeSeries.Unit
        };
    }

    private static SelectionPowerAnalyticsResult BuildSelectionPowerAnalytics(
        SelectionSignalAvailabilityItem? selectionSignal,
        CuratedNodeTimeSeriesResult? timeSeries)
    {
        const string methodology = "Near-base = 5th percentile of the active power series. Near-peak = 95th percentile of the same series. Peak-base ratio = near-peak / near-base when near-base is numerically safe. On-hour duration counts buckets at or above the midpoint between near-base and near-peak. After-hours Load reuses that same threshold only for fixed after-hours buckets (weekday outside 07:00-19:00 UTC plus weekends). Base vs Peak Over Time applies the same 5th/95th percentile basis per UTC day and requires sub-daily power samples. Load duration curve sorts the same active power series in descending order without falling back to another signal.";
        const string distinctionNote = "On-hour duration shows how long the active power series stays in its higher-load mode across the whole interval. After-hours Load uses the same threshold, but only inside fixed after-hours windows, so it reads as after-hours persistence rather than total runtime.";

        if (selectionSignal is null)
        {
            return new SelectionPowerAnalyticsResult
            {
                Summary = "Power Analytics wait for an active exact signal code.",
                Methodology = methodology,
                DistinctionNote = distinctionNote
            };
        }

        if (selectionSignal.SignalFamily != FacilitySignalFamily.Power)
        {
            return new SelectionPowerAnalyticsResult
            {
                Summary = $"Power Analytics are available only for the power family (P, P1, P2, P3). The current active signal {selectionSignal.ExactSignalCode.Value} belongs to {selectionSignal.SignalFamily}.",
                Methodology = methodology,
                DistinctionNote = distinctionNote
            };
        }

        var loadDurationCurve = BuildSelectionPowerLoadDurationCurveSummary(timeSeries, selectionSignal);
        if (timeSeries is null)
        {
            return new SelectionPowerAnalyticsResult
            {
                IsPowerSignal = true,
                Summary = "Power Analytics are unavailable because the active power series could not be resolved.",
                Methodology = methodology,
                DistinctionNote = distinctionNote,
                LoadDurationCurve = loadDurationCurve
            };
        }

        var unit = ResolveSelectionPowerUnit(timeSeries);
        var evaluationBasis = DescribeSelectionPowerEvaluationBasis(timeSeries, selectionSignal);

        if (timeSeries.Points.Count == 0)
        {
            return new SelectionPowerAnalyticsResult
            {
                IsPowerSignal = true,
                Unit = unit,
                EvaluationBasis = evaluationBasis,
                Summary = timeSeries.NoDataMessage ?? "Power Analytics are unavailable because the active power series has no points in the current interval.",
                Methodology = methodology,
                DistinctionNote = distinctionNote,
                LoadDurationCurve = loadDurationCurve
            };
        }

        var values = timeSeries.Points
            .Select(point => point.Value)
            .ToArray();
        if (IsMixedSignAggregateSelectionPowerSeries(selectionSignal, values))
        {
            var mixedSignSummary = "Power Analytics are intentionally unavailable because the current active aggregate P series is mixed-sign.";
            var mixedSignReason = "Load-shape power analytics assume a consumption-oriented, non-mixed-sign power basis. Choose a non-mixed-sign signal or narrower scope instead of silently switching signals.";
            var mixedSignEvaluationBasis = $"{evaluationBasis} Current aggregate P contains both positive and negative values.";

            return new SelectionPowerAnalyticsResult
            {
                IsPowerSignal = true,
                IsMixedSignAggregateUnavailable = true,
                PointCount = values.Length,
                Unit = unit,
                EvaluationBasis = mixedSignEvaluationBasis,
                Summary = mixedSignSummary,
                Methodology = methodology,
                DistinctionNote = distinctionNote,
                NearBase = BuildUnavailableSelectionPowerMetric(unit, mixedSignSummary, mixedSignReason),
                NearPeak = BuildUnavailableSelectionPowerMetric(unit, mixedSignSummary, mixedSignReason),
                PeakBaseRatio = BuildUnavailableSelectionPowerMetric("x", mixedSignSummary, mixedSignReason),
                OnHourDuration = BuildUnavailableSelectionPowerDuration(mixedSignSummary, mixedSignReason),
                AfterHoursLoad = BuildUnavailableSelectionPowerDuration(mixedSignSummary, mixedSignReason),
                BasePeakOverTime = BuildUnavailableSelectionPowerBasePeakOverTime(
                    selectionSignal.ExactSignalCode.Value,
                    unit,
                    timeSeries.GranularityLabel,
                    mixedSignEvaluationBasis,
                    values.Select(static point => point).Count(),
                    mixedSignSummary,
                    mixedSignReason),
                LoadDurationCurve = BuildUnavailableSelectionPowerLoadDurationCurve(
                    unit,
                    timeSeries,
                    mixedSignEvaluationBasis,
                    mixedSignSummary,
                    mixedSignReason)
            };
        }

        var nearBase = Percentile(values, 0.05);
        var nearPeak = Percentile(values, 0.95);
        var threshold = (nearBase + nearPeak) / 2d;
        var safeRatioFloor = GetMinimumSafePowerRatioDenominator(values);
        var canComputeRatio = Math.Abs(nearBase) > safeRatioFloor;
        var unitSuffix = string.IsNullOrWhiteSpace(unit)
            ? string.Empty
            : $" {unit}";
        var onHourDuration = BuildSelectionOnHourDurationSummary(timeSeries, threshold, unit);
        var afterHoursLoad = BuildSelectionPowerAfterHoursLoadSummary(timeSeries, threshold, unit);
        var basePeakOverTime = BuildSelectionPowerBasePeakOverTimeSummary(timeSeries, selectionSignal, unit, evaluationBasis);

        return new SelectionPowerAnalyticsResult
        {
            IsPowerSignal = true,
            IsAvailable = true,
            PointCount = values.Length,
            Unit = unit,
            EvaluationBasis = evaluationBasis,
            Summary = $"Power Analytics are computed from the same {values.Length}-point {selectionSignal.ExactSignalCode.Value} series used by Signal Analytics.",
            Methodology = methodology,
            DistinctionNote = distinctionNote,
            NearBase = new SelectionPowerMetricSummary
            {
                IsAvailable = true,
                State = CuratedPerformanceKpiState.Available,
                Value = nearBase,
                Unit = unit,
                Summary = "Near-base = 5th percentile of the current active power series.",
                StateReason = "Robust lower-load estimate from the exact active signal."
            },
            NearPeak = new SelectionPowerMetricSummary
            {
                IsAvailable = true,
                State = CuratedPerformanceKpiState.Available,
                Value = nearPeak,
                Unit = unit,
                Summary = "Near-peak = 95th percentile of the current active power series.",
                StateReason = "Robust upper-load estimate from the exact active signal."
            },
            PeakBaseRatio = canComputeRatio
                ? new SelectionPowerMetricSummary
                {
                    IsAvailable = true,
                    State = CuratedPerformanceKpiState.Available,
                    Value = nearPeak / nearBase,
                    Unit = "x",
                    Summary = "Peak-base ratio = near-peak / near-base over the same active power series.",
                    StateReason = "Computed without fallback from the exact active signal."
                }
                : new SelectionPowerMetricSummary
                {
                    IsAvailable = false,
                    State = CuratedPerformanceKpiState.Unavailable,
                    Unit = "x",
                    Summary = "Peak-base ratio cannot be computed safely because near-base is too close to zero.",
                    StateReason = $"Near-base {nearBase.ToString("N2", CultureInfo.InvariantCulture)}{unitSuffix} is below the safe division floor for this series."
                },
            OnHourDuration = onHourDuration,
            AfterHoursLoad = afterHoursLoad,
                BasePeakOverTime = basePeakOverTime,
            LoadDurationCurve = loadDurationCurve
        };
    }

    private static SelectionPowerMetricSummary BuildUnavailableSelectionPowerMetric(
        string unit,
        string summary,
        string stateReason)
        => new()
        {
            IsAvailable = false,
            State = CuratedPerformanceKpiState.Unavailable,
            Unit = unit,
            Summary = summary,
            StateReason = stateReason
        };

    private static SelectionPowerDurationSummary BuildUnavailableSelectionPowerDuration(
        string summary,
        string stateReason)
        => new()
        {
            IsAvailable = false,
            State = CuratedPerformanceKpiState.Unavailable,
            Summary = summary,
            StateReason = stateReason
        };

    private static SelectionPowerBasePeakOverTimeResult BuildUnavailableSelectionPowerBasePeakOverTime(
        string signalCode,
        string unit,
        string granularityLabel,
        string evaluationBasis,
        int totalDayCount,
        string summary,
        string stateReason)
        => new()
        {
            IsAvailable = false,
            State = CuratedPerformanceKpiState.Unavailable,
            SignalCode = signalCode,
            Unit = unit,
            GranularityLabel = granularityLabel,
            TotalDayCount = totalDayCount,
            EvaluationBasis = evaluationBasis,
            Summary = summary,
            StateReason = stateReason,
            Chart = new CuratedNodeCompareTimeSeriesResult
            {
                PrimaryNodeKey = "selection_power_base_peak",
                Title = "Base vs Peak Over Time",
                Unit = unit,
                YAxisLabel = string.IsNullOrWhiteSpace(unit)
                    ? "Power"
                    : $"Power ({unit})",
                Granularity = CuratedNodeTimeSeriesGranularity.DailyAverage,
                GranularityLabel = "Daily 5th/95th percentiles",
                AggregationMethod = "Unavailable",
                InterpretationNote = stateReason,
                NoDataMessage = summary
            }
        };

    private static CuratedSelectionLoadDurationCurveSummary BuildUnavailableSelectionPowerLoadDurationCurve(
        string unit,
        CuratedNodeTimeSeriesResult timeSeries,
        string evaluationBasis,
        string summary,
        string stateReason)
        => new()
        {
            IsAvailable = false,
            State = CuratedPerformanceKpiState.Unavailable,
            Unit = unit,
            YAxisLabel = string.IsNullOrWhiteSpace(timeSeries.YAxisLabel)
                ? (string.IsNullOrWhiteSpace(unit) ? "Power" : $"Power ({unit})")
                : timeSeries.YAxisLabel,
            EvaluationBasis = evaluationBasis,
            Summary = summary,
            StateReason = stateReason,
            Methodology = "Load duration curve is hidden for mixed-sign aggregate P because this load-shape block does not silently project net power to demand-positive values or switch to another signal."
        };

    private static bool IsMixedSignAggregateSelectionPowerSeries(
        SelectionSignalAvailabilityItem selectionSignal,
        IReadOnlyList<double> values)
        => selectionSignal.CanAggregate
            && selectionSignal.ExactSignalCode == FacilitySignalCode.P
            && values.Any(value => value > 0d)
            && values.Any(value => value < 0d);

    private static SelectionPowerDurationSummary BuildSelectionOnHourDurationSummary(
        CuratedNodeTimeSeriesResult timeSeries,
        double threshold,
        string unit)
    {
        var bucketDurationHours = ResolveSelectionPowerBucketDurationHours(timeSeries);
        var matchingBucketCount = timeSeries.Points.Count(point => point.Value >= threshold);
        var totalBucketCount = timeSeries.Points.Count;
        var shareRatio = totalBucketCount == 0
            ? (double?)null
            : matchingBucketCount / (double)totalBucketCount;

        return new SelectionPowerDurationSummary
        {
            IsAvailable = true,
            State = CuratedPerformanceKpiState.Available,
            DurationHours = matchingBucketCount * bucketDurationHours,
            ShareRatio = shareRatio,
            ThresholdValue = threshold,
            ThresholdUnit = unit,
            MatchingBucketCount = matchingBucketCount,
            TotalBucketCount = totalBucketCount,
            Summary = $"On-hour duration counts buckets at or above the midpoint threshold between near-base and near-peak. {matchingBucketCount} of {totalBucketCount} buckets are in the higher-load mode.",
            StateReason = $"Threshold = midpoint(near-base, near-peak) = {threshold.ToString("N2", CultureInfo.InvariantCulture)}{FormatSelectionPowerUnitSuffix(unit)}."
        };
    }

    private static SelectionPowerDurationSummary BuildSelectionPowerAfterHoursLoadSummary(
        CuratedNodeTimeSeriesResult timeSeries,
        double threshold,
        string unit)
    {
        if (timeSeries.Granularity == CuratedNodeTimeSeriesGranularity.DailyAverage)
        {
            return new SelectionPowerDurationSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                ThresholdValue = threshold,
                ThresholdUnit = unit,
                Summary = "After-hours Load is unavailable because the current active power series is only available as daily buckets.",
                StateReason = "This metric keeps fixed after-hours windows (weekday outside 07:00-19:00 UTC plus weekends), so it requires sub-daily timestamps."
            };
        }

        var afterHoursPoints = timeSeries.Points
            .Where(point => IsSelectionPowerAfterHoursBucket(point.TimestampUtc))
            .ToList();

        if (afterHoursPoints.Count == 0)
        {
            return new SelectionPowerDurationSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                ThresholdValue = threshold,
                ThresholdUnit = unit,
                Summary = "After-hours Load is unavailable because the current interval does not contain any fixed after-hours buckets.",
                StateReason = "After-hours windows use weekday timestamps outside 07:00-19:00 UTC plus all weekend timestamps."
            };
        }

        var bucketDurationHours = ResolveSelectionPowerBucketDurationHours(timeSeries);
        var matchingBucketCount = afterHoursPoints.Count(point => point.Value >= threshold);

        return new SelectionPowerDurationSummary
        {
            IsAvailable = true,
            State = CuratedPerformanceKpiState.Available,
            DurationHours = matchingBucketCount * bucketDurationHours,
            ShareRatio = matchingBucketCount / (double)afterHoursPoints.Count,
            ThresholdValue = threshold,
            ThresholdUnit = unit,
            MatchingBucketCount = matchingBucketCount,
            TotalBucketCount = afterHoursPoints.Count,
            Summary = $"After-hours Load reuses the on-hour threshold, but only for fixed after-hours windows. {matchingBucketCount} of {afterHoursPoints.Count} after-hours buckets stay at or above that threshold.",
            StateReason = "After-hours windows = weekday outside 07:00-19:00 UTC plus weekends; this is a persistence metric, not a separate heuristic baseline."
        };
    }

    private static bool IsSelectionPowerAfterHoursBucket(DateTime timestampUtc)
        => timestampUtc.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            || timestampUtc.Hour < 7
            || timestampUtc.Hour >= 19;

    private static SelectionPowerBasePeakOverTimeResult BuildSelectionPowerBasePeakOverTimeSummary(
        CuratedNodeTimeSeriesResult timeSeries,
        SelectionSignalAvailabilityItem selectionSignal,
        string unit,
        string evaluationBasis)
    {
        const int minimumUsableDayCount = 3;
        const string methodology = "Base over time = daily 5th percentile of the active power series. Peak over time = daily 95th percentile of the same series. Each usable day requires sub-daily power samples; no fallback signal is used.";

        var signalCode = selectionSignal.ExactSignalCode.Value;
        var totalDayCount = timeSeries.Points
            .Select(point => DateTime.SpecifyKind(point.TimestampUtc.Date, DateTimeKind.Utc))
            .Distinct()
            .Count();

        if (timeSeries.Granularity == CuratedNodeTimeSeriesGranularity.DailyAverage)
        {
            return BuildUnavailableSelectionPowerBasePeakOverTime(
                signalCode,
                unit,
                timeSeries.GranularityLabel,
                evaluationBasis,
                totalDayCount,
                "Base vs Peak Over Time is unavailable because the current active power series is only available as daily buckets.",
                "This chart requires sub-daily power samples for each usable UTC day, so daily-only buckets cannot preserve the daily 5th and 95th percentiles.");
        }

        var usableDays = timeSeries.Points
            .GroupBy(point => DateTime.SpecifyKind(point.TimestampUtc.Date, DateTimeKind.Utc))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var dayValues = group
                    .Select(point => point.Value)
                    .ToArray();

                return new SelectionPowerBasePeakDaySummary
                {
                    DayUtc = group.Key,
                    SampleCount = dayValues.Length,
                    BaseValue = Percentile(dayValues, 0.05),
                    PeakValue = Percentile(dayValues, 0.95)
                };
            })
            .ToList();

        if (usableDays.Count < minimumUsableDayCount)
        {
            return BuildUnavailableSelectionPowerBasePeakOverTime(
                signalCode,
                unit,
                timeSeries.GranularityLabel,
                $"{evaluationBasis} Daily percentiles need at least {minimumUsableDayCount} usable UTC days.",
                totalDayCount,
                $"Base vs Peak Over Time is unavailable because the current interval contains only {usableDays.Count} usable day(s).",
                "At least 3 usable UTC days with sub-daily power samples are required for this MVP chart.");
        }

        var yAxisLabel = string.IsNullOrWhiteSpace(timeSeries.YAxisLabel)
            ? (string.IsNullOrWhiteSpace(unit) ? "Power" : $"Power ({unit})")
            : timeSeries.YAxisLabel;

        return new SelectionPowerBasePeakOverTimeResult
        {
            IsAvailable = true,
            State = CuratedPerformanceKpiState.Available,
            UsableDayCount = usableDays.Count,
            TotalDayCount = totalDayCount,
            SignalCode = signalCode,
            Unit = unit,
            GranularityLabel = timeSeries.GranularityLabel,
            EvaluationBasis = $"{evaluationBasis} Daily Base_d and Peak_d are built per UTC day from sub-daily samples of the same active signal.",
            Summary = $"Base vs Peak Over Time tracks daily 5th and 95th percentiles across {usableDays.Count} usable UTC day(s) of the active {signalCode} power series.",
            StateReason = "Daily percentiles are computed directly from the active exact signal without silently switching to another basis.",
            Methodology = methodology,
            Days = usableDays,
            Chart = new CuratedNodeCompareTimeSeriesResult
            {
                PrimaryNodeKey = "selection_power_base_peak",
                Title = "Base vs Peak Over Time",
                Unit = unit,
                YAxisLabel = yAxisLabel,
                Granularity = CuratedNodeTimeSeriesGranularity.DailyAverage,
                GranularityLabel = "Daily 5th/95th percentiles",
                AggregationMethod = "Each UTC day is reduced to Base_d = 5th percentile and Peak_d = 95th percentile over the active power series.",
                InterpretationNote = "Base over time = daily 5th percentile of the active power series. Peak over time = daily 95th percentile of the same series.",
                RequestedMode = timeSeries.RequestedMode,
                RequestedModeLabel = timeSeries.RequestedModeLabel,
                Series =
                [
                    new CuratedNodeCompareSeries
                    {
                        NodeKey = "base_over_time",
                        Label = "Base over time",
                        IsPrimary = true,
                        Points = usableDays
                            .Select(day => new CuratedNodeTimeSeriesPoint
                            {
                                TimestampUtc = day.DayUtc,
                                Value = day.BaseValue
                            })
                            .ToArray()
                    },
                    new CuratedNodeCompareSeries
                    {
                        NodeKey = "peak_over_time",
                        Label = "Peak over time",
                        Points = usableDays
                            .Select(day => new CuratedNodeTimeSeriesPoint
                            {
                                TimestampUtc = day.DayUtc,
                                Value = day.PeakValue
                            })
                            .ToArray()
                    }
                ]
            }
        };
    }

    private static double ResolveSelectionPowerBucketDurationHours(CuratedNodeTimeSeriesResult timeSeries)
    {
        if (timeSeries.Points.Count >= 2)
        {
            var intervalsInHours = timeSeries.Points
                .Zip(timeSeries.Points.Skip(1), (current, next) => (next.TimestampUtc - current.TimestampUtc).TotalHours)
                .Where(interval => interval > 0d)
                .OrderBy(interval => interval)
                .ToList();

            if (intervalsInHours.Count > 0)
            {
                return intervalsInHours[intervalsInHours.Count / 2];
            }
        }

        return timeSeries.Granularity switch
        {
            CuratedNodeTimeSeriesGranularity.Raw15Min => 0.25d,
            CuratedNodeTimeSeriesGranularity.HourlyAverage => 1d,
            CuratedNodeTimeSeriesGranularity.DailyAverage => 24d,
            _ => 1d
        };
    }

    private static string FormatSelectionPowerUnitSuffix(string unit)
        => string.IsNullOrWhiteSpace(unit)
            ? string.Empty
            : $" {unit}";

    private static CuratedSelectionLoadDurationCurveSummary BuildSelectionPowerLoadDurationCurveSummary(
        CuratedNodeTimeSeriesResult? timeSeries,
        SelectionSignalAvailabilityItem selectionSignal)
    {
        if (timeSeries is null)
        {
            return new CuratedSelectionLoadDurationCurveSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                StateReason = "The active power series could not be resolved.",
                EvaluationBasis = $"Active exact signal: {selectionSignal.ExactSignalCode.Value}.",
                Summary = "Load duration curve is unavailable because the active power series is missing.",
                Methodology = "Load duration curve sorts the exact active power series values in descending order over the current interval. No fallback signal is used."
            };
        }

        var unit = ResolveSelectionPowerUnit(timeSeries);
        var yAxisLabel = string.IsNullOrWhiteSpace(timeSeries.YAxisLabel)
            ? (string.IsNullOrWhiteSpace(unit) ? "Power" : $"Power ({unit})")
            : timeSeries.YAxisLabel;
        var evaluationBasis = DescribeSelectionPowerEvaluationBasis(timeSeries, selectionSignal);

        if (timeSeries.Points.Count == 0)
        {
            return new CuratedSelectionLoadDurationCurveSummary
            {
                IsAvailable = false,
                State = CuratedPerformanceKpiState.Unavailable,
                Unit = unit,
                YAxisLabel = yAxisLabel,
                StateReason = "The active power series does not contain any points in the current interval.",
                EvaluationBasis = evaluationBasis,
                Summary = timeSeries.NoDataMessage ?? "Load duration curve is unavailable because the active power series has no points.",
                Methodology = "Load duration curve sorts the exact active power series values in descending order over the current interval. No fallback signal is used."
            };
        }

        var orderedValues = timeSeries.Points
            .Select(point => point.Value)
            .OrderByDescending(value => value)
            .ToList();
        var curvePoints = new List<CuratedSelectionLoadDurationCurvePoint>(orderedValues.Count);

        for (int index = 0; index < orderedValues.Count; index++)
        {
            var durationPercent = orderedValues.Count == 1
                ? 0d
                : (index / (double)(orderedValues.Count - 1)) * 100d;

            curvePoints.Add(new CuratedSelectionLoadDurationCurvePoint
            {
                DurationPercent = durationPercent,
                DemandKw = orderedValues[index]
            });
        }

        return new CuratedSelectionLoadDurationCurveSummary
        {
            IsAvailable = true,
            State = CuratedPerformanceKpiState.Available,
            Unit = unit,
            YAxisLabel = yAxisLabel,
            StateReason = "Load duration curve is computed from the same active power series used for near-base and near-peak.",
            EvaluationBasis = evaluationBasis,
            PointCount = orderedValues.Count,
            PeakDemandKw = orderedValues[0],
            AverageDemandKw = orderedValues.Average(),
            Summary = $"Load duration curve ranks {orderedValues.Count} values from the active {selectionSignal.ExactSignalCode.Value} series in descending order.",
            Methodology = "Load duration curve sorts the exact active power series values in descending order over the current interval. No demand-positive projection or fallback signal is applied.",
            Points = curvePoints
        };
    }

    private static string DescribeSelectionPowerEvaluationBasis(
        CuratedNodeTimeSeriesResult timeSeries,
        SelectionSignalAvailabilityItem selectionSignal)
    {
        var scopeLabel = selectionSignal.CanAggregate
            ? $"aggregate sum across {selectionSignal.MatchingNodeCount} matching nodes"
            : "single-node exact-signal series";

        return $"Evaluation basis: {timeSeries.GranularityLabel} from the active {selectionSignal.ExactSignalCode.Value} power series ({scopeLabel}).";
    }

    private static string ResolveSelectionPowerUnit(CuratedNodeTimeSeriesResult timeSeries)
        => string.IsNullOrWhiteSpace(timeSeries.Unit)
            ? "kW"
            : timeSeries.Unit;

    private enum IntervalEnergyBasisKind
    {
        DirectEnergy,
        CumulativeEnergyCounter,
        IntegratedPower
    }

    private enum IntervalEnergyBasisFailureKind
    {
        MissingUsableEnergyBasis,
        CannotIntegrateSafely
    }

    private sealed record ScopeFloorAreaResolution(
        bool IsResolved,
        double? FloorAreaM2,
        string Summary,
        string StateReason,
        string EvaluationBasis,
        string? Message,
        string? FloorAreaNodeKey);

    private sealed record IntervalEnergyNodeResult(
        bool IsAvailable,
        double EnergyKwh,
        IntervalEnergyBasisKind? BasisKind,
        IntervalEnergyBasisFailureKind? FailureKind,
        string? FailureMessage);

    private sealed record IntervalEnergyBasisResolution(
        bool IsResolved,
        double EnergyKwh,
        int ContributingNodeCount,
        string EvaluationBasis,
        IntervalEnergyBasisFailureKind? FailureKind,
        string? FailureMessage);

    private async Task<SelectionEuiResult> BuildSelectionEuiAsync(
        SelectionSignalAvailabilityItem? selectionSignal,
        IReadOnlyList<string> scopeNodeKeys,
        string? scopeAnchorNodeKey,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        if (selectionSignal is null)
        {
            return new SelectionEuiResult
            {
                Summary = "EUI waits for an active exact signal code.",
                Methodology = EuiMethodology
            };
        }

        if (selectionSignal.SignalFamily is not (FacilitySignalFamily.Energy or FacilitySignalFamily.Power))
        {
            return new SelectionEuiResult
            {
                IsApplicable = false,
                SignalCode = selectionSignal.ExactSignalCode.Value,
                Summary = $"EUI requires an energy or power signal. The active signal {selectionSignal.ExactSignalCode.Value} belongs to {selectionSignal.SignalFamily}.",
                Methodology = EuiMethodology
            };
        }

        var floorAreaResolution = await ResolveSelectionScopeFloorAreaAsync(scopeAnchorNodeKey, scopeNodeKeys, ct);
        if (!floorAreaResolution.IsResolved)
        {
            return new SelectionEuiResult
            {
                IsApplicable = true,
                SignalCode = selectionSignal.ExactSignalCode.Value,
                FloorAreaM2 = floorAreaResolution.FloorAreaM2,
                Summary = floorAreaResolution.Summary,
                StateReason = floorAreaResolution.StateReason,
                EvaluationBasis = floorAreaResolution.EvaluationBasis,
                Methodology = EuiMethodology,
                Message = floorAreaResolution.Message,
                FloorAreaNodeKey = floorAreaResolution.FloorAreaNodeKey
            };
        }

        if (!selectionSignal.CanAggregate && !selectionSignal.CanRenderSingleNodeSeries)
        {
            return new SelectionEuiResult
            {
                IsApplicable = true,
                SignalCode = selectionSignal.ExactSignalCode.Value,
                FloorAreaM2 = floorAreaResolution.FloorAreaM2,
                FloorAreaNodeKey = floorAreaResolution.FloorAreaNodeKey,
                Summary = "EUI is unavailable because the active signal has no usable energy basis in the current scope.",
                StateReason = "missing usable energy basis",
                EvaluationBasis = floorAreaResolution.EvaluationBasis,
                Methodology = EuiMethodology,
                Message = selectionSignal.AvailabilityMessage
            };
        }

        var intervalEnergy = await BuildSelectionIntervalEnergyBasisAsync(selectionSignal, from, to, ct);
        if (!intervalEnergy.IsResolved)
        {
            var stateReason = intervalEnergy.FailureKind == IntervalEnergyBasisFailureKind.CannotIntegrateSafely
                ? "unsafe integration"
                : "missing usable energy basis";
            var summary = intervalEnergy.FailureKind == IntervalEnergyBasisFailureKind.CannotIntegrateSafely
                ? "EUI is unavailable because interval energy cannot be integrated safely from the active signal basis."
                : "EUI is unavailable because the active signal does not expose a usable energy basis in the selected interval.";

            return new SelectionEuiResult
            {
                IsApplicable = true,
                SignalCode = selectionSignal.ExactSignalCode.Value,
                FloorAreaM2 = floorAreaResolution.FloorAreaM2,
                FloorAreaNodeKey = floorAreaResolution.FloorAreaNodeKey,
                Summary = summary,
                StateReason = stateReason,
                EvaluationBasis = JoinEvaluationBasis(intervalEnergy.EvaluationBasis, floorAreaResolution.EvaluationBasis),
                Methodology = EuiMethodology,
                Message = intervalEnergy.FailureMessage
            };
        }

        var floorArea = floorAreaResolution.FloorAreaM2.GetValueOrDefault();
        return new SelectionEuiResult
        {
            IsApplicable = true,
            IsAvailable = true,
            EnergyKwh = intervalEnergy.EnergyKwh,
            FloorAreaM2 = floorArea,
            EuiKwhPerM2 = intervalEnergy.EnergyKwh / floorArea,
            ContributingNodeCount = intervalEnergy.ContributingNodeCount,
            SignalCode = selectionSignal.ExactSignalCode.Value,
            Summary = $"Period EUI over the selected interval is computed from {intervalEnergy.ContributingNodeCount} contributing node(s) using the active {selectionSignal.ExactSignalCode.Value} signal.",
            Methodology = EuiMethodology,
            EvaluationBasis = JoinEvaluationBasis(intervalEnergy.EvaluationBasis, floorAreaResolution.EvaluationBasis),
            StateReason = "period EUI over selected interval",
            FloorAreaNodeKey = floorAreaResolution.FloorAreaNodeKey
        };
    }

    private async Task<ScopeFloorAreaResolution> ResolveSelectionScopeFloorAreaAsync(
        string? scopeAnchorNodeKey,
        IReadOnlyList<string> scopeNodeKeys,
        CancellationToken ct)
    {
        var nodeStates = await _facilityEditorStateService.GetNodeStatesByKeyAsync(ct);
        var facility = await _facilityQueryService.GetMainFacilityAsync(ct);
        var facilityNodesByKey = facility?.Nodes?
            .ToDictionary(node => node.NodeKey, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, DiplomovaPrace.Persistence.Schematic.SchematicNodeEntity>(StringComparer.OrdinalIgnoreCase);

        var candidateKeys = new List<string>();
        if (!string.IsNullOrWhiteSpace(scopeAnchorNodeKey))
        {
            candidateKeys.Add(scopeAnchorNodeKey.Trim());
        }

        foreach (var nodeKey in scopeNodeKeys)
        {
            if (string.IsNullOrWhiteSpace(nodeKey) || candidateKeys.Contains(nodeKey, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            candidateKeys.Add(nodeKey);
        }

        var areaCandidates = candidateKeys
            .Where(nodeKey => IsAreaNodeType(ResolveEffectiveNodeType(nodeKey, facilityNodesByKey, nodeStates)))
            .Select(nodeKey =>
            {
                nodeStates.TryGetValue(nodeKey, out var nodeState);
                return new
                {
                    NodeKey = nodeKey,
                    FloorAreaM2 = nodeState?.FloorAreaM2
                };
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(scopeAnchorNodeKey)
            && areaCandidates.FirstOrDefault(candidate => candidate.NodeKey.Equals(scopeAnchorNodeKey, StringComparison.OrdinalIgnoreCase)) is { } anchorAreaCandidate)
        {
            if (!anchorAreaCandidate.FloorAreaM2.HasValue)
            {
                return new ScopeFloorAreaResolution(
                    false,
                    null,
                    "EUI is unavailable because the selected scope is missing floor area metadata.",
                    "missing floor area",
                    $"Floor area scope: selected area node {anchorAreaCandidate.NodeKey} has no explicit Floor area [m²] metadata.",
                    "Enter Floor area [m²] on the selected area node to enable Energy / FloorArea.",
                    anchorAreaCandidate.NodeKey);
            }

            if (anchorAreaCandidate.FloorAreaM2.Value <= 0d)
            {
                return new ScopeFloorAreaResolution(
                    false,
                    anchorAreaCandidate.FloorAreaM2,
                    "EUI is unavailable because floor area must be greater than 0 m².",
                    "invalid floor area",
                    $"Floor area scope: selected area node {anchorAreaCandidate.NodeKey} is set to {anchorAreaCandidate.FloorAreaM2.Value:N2} m².",
                    "Update Floor area [m²] to a value greater than 0 on the selected area node.",
                    anchorAreaCandidate.NodeKey);
            }

            return new ScopeFloorAreaResolution(
                true,
                anchorAreaCandidate.FloorAreaM2,
                string.Empty,
                string.Empty,
                $"Floor area scope: {anchorAreaCandidate.FloorAreaM2.Value:N2} m² from area node {anchorAreaCandidate.NodeKey}.",
                null,
                anchorAreaCandidate.NodeKey);
        }

        var validAreaCandidates = areaCandidates
            .Where(candidate => candidate.FloorAreaM2 is double floorAreaM2 && floorAreaM2 > 0d)
            .ToList();

        if (validAreaCandidates.Count == 1)
        {
            var candidate = validAreaCandidates[0];
            var floorAreaM2 = candidate.FloorAreaM2.GetValueOrDefault();
            return new ScopeFloorAreaResolution(
                true,
                floorAreaM2,
                string.Empty,
                string.Empty,
                $"Floor area scope: {floorAreaM2:N2} m² from area node {candidate.NodeKey}.",
                null,
                candidate.NodeKey);
        }

        if (validAreaCandidates.Count > 1)
        {
            return new ScopeFloorAreaResolution(
                false,
                null,
                "EUI is unavailable because the selected scope exposes multiple floor area candidates.",
                "multiple floor area candidates",
                $"Floor area scope: {validAreaCandidates.Count} area nodes in the current scope contain explicit Floor area [m²] metadata.",
                "Refine the selection to one area scope or keep the intended area node focused before evaluating EUI.",
                null);
        }

        if (areaCandidates.Any(candidate => candidate.FloorAreaM2 is double floorAreaM2 && floorAreaM2 <= 0d))
        {
            var invalidAreaCandidate = areaCandidates.First(candidate => candidate.FloorAreaM2 is double floorAreaM2 && floorAreaM2 <= 0d);
            var floorAreaM2 = invalidAreaCandidate.FloorAreaM2.GetValueOrDefault();
            return new ScopeFloorAreaResolution(
                false,
                floorAreaM2,
                "EUI is unavailable because floor area must be greater than 0 m².",
                "invalid floor area",
                $"Floor area scope: area node {invalidAreaCandidate.NodeKey} is set to {floorAreaM2:N2} m².",
                "Update Floor area [m²] to a value greater than 0 on the intended area node.",
                invalidAreaCandidate.NodeKey);
        }

        return new ScopeFloorAreaResolution(
            false,
            null,
            "EUI is unavailable because the selected scope is missing floor area metadata.",
            "missing floor area",
            areaCandidates.Count > 0
                ? "Floor area scope: area nodes are present in the current scope, but none has explicit Floor area [m²] metadata."
                : "Floor area scope: the current selection does not expose an area node with explicit Floor area [m²] metadata.",
            "EUI requires explicit Floor area [m²] metadata on the selected area scope. No default or inferred area is used.",
            null);
    }

    private async Task<IntervalEnergyBasisResolution> BuildSelectionIntervalEnergyBasisAsync(
        SelectionSignalAvailabilityItem selectionSignal,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var candidateNodeResults = new List<(string NodeKey, IntervalEnergyNodeResult Result)>();
        var missingBasisCount = 0;
        var unsafeIntegrationCount = 0;

        foreach (var nodeKey in selectionSignal.MatchingNodeKeys)
        {
            var source = ResolveCuratedNodeSource(nodeKey, selectionSignal.ExactSignalCode);
            if (source is null)
            {
                missingBasisCount++;
                continue;
            }

            var filePath = ResolveCuratedFilePath(source);
            if (filePath is null)
            {
                missingBasisCount++;
                continue;
            }

            var intervalEnergy = await LoadIntervalEnergyForSourceAsync(filePath, source, from, to, ct);
            if (!intervalEnergy.IsAvailable)
            {
                if (intervalEnergy.FailureKind == IntervalEnergyBasisFailureKind.CannotIntegrateSafely)
                {
                    unsafeIntegrationCount++;
                }
                else
                {
                    missingBasisCount++;
                }

                continue;
            }

            candidateNodeResults.Add((nodeKey, intervalEnergy));
        }

        if (candidateNodeResults.Count == 0)
        {
            var failureKind = unsafeIntegrationCount > 0
                ? IntervalEnergyBasisFailureKind.CannotIntegrateSafely
                : IntervalEnergyBasisFailureKind.MissingUsableEnergyBasis;
            var failureMessage = failureKind == IntervalEnergyBasisFailureKind.CannotIntegrateSafely
                ? "The active signal exists, but interval energy cannot be derived safely from the available timestamp spacing in the selected interval."
                : "No matching node exposes a usable energy basis in the selected interval.";

            return new IntervalEnergyBasisResolution(
                false,
                0d,
                0,
                string.Empty,
                failureKind,
                failureMessage);
        }

        var basisKinds = candidateNodeResults
            .Select(result => result.Result.BasisKind)
            .Where(kind => kind.HasValue)
            .Select(kind => kind!.Value)
            .Distinct()
            .ToList();

        var basisDescription = DescribeIntervalEnergyBasis(basisKinds);
        var evaluationBasis = selectionSignal.CanAggregate
            ? $"Energy basis: {basisDescription} aggregated across {candidateNodeResults.Count} of {selectionSignal.MatchingNodeCount} matching nodes for {selectionSignal.ExactSignalCode.Value}."
            : $"Energy basis: {basisDescription} from the active exact signal {selectionSignal.ExactSignalCode.Value} on the current scope node.";

        if (candidateNodeResults.Count < selectionSignal.MatchingNodeCount)
        {
            evaluationBasis = $"{evaluationBasis} {selectionSignal.MatchingNodeCount - candidateNodeResults.Count} matching node(s) were skipped because no safe interval energy basis was available in the selected interval.";
        }

        return new IntervalEnergyBasisResolution(
            true,
            candidateNodeResults.Sum(result => result.Result.EnergyKwh),
            candidateNodeResults.Count,
            evaluationBasis,
            null,
            null);
    }

    private async Task<IntervalEnergyNodeResult> LoadIntervalEnergyForSourceAsync(
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        if (source.SeriesSemantics == FacilitySignalSeriesSemantics.CumulativeCounter)
        {
            return await LoadIntervalEnergyFromCounterSourceAsync(filePath, source, from, to, ct);
        }

        if (source.IsPowerSignal)
        {
            return await LoadIntervalEnergyFromPowerSourceAsync(filePath, source, from, to, ct);
        }

        return await LoadIntervalEnergyFromDirectEnergySourceAsync(filePath, source, from, to, ct);
    }

    private async Task<IntervalEnergyNodeResult> LoadIntervalEnergyFromPowerSourceAsync(
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var samples = await LoadRawSamplesWithContextAsync(filePath, source.ColumnName, from, to, ct);
        var totalEnergyKwh = 0d;
        var integratedSegmentCount = 0;

        for (var index = 0; index < samples.Count - 1; index++)
        {
            var current = samples[index];
            var next = samples[index + 1];
            if (next.TimestampUtc <= current.TimestampUtc)
            {
                continue;
            }

            var segmentStart = MaxDateTime(current.TimestampUtc, from);
            var segmentEnd = MinDateTime(next.TimestampUtc, to);
            if (segmentEnd <= segmentStart)
            {
                continue;
            }

            var powerKw = NormalizeValueForStats(current.Value, source);
            if (!double.IsFinite(powerKw))
            {
                continue;
            }

            totalEnergyKwh += powerKw * (segmentEnd - segmentStart).TotalHours;
            integratedSegmentCount++;
        }

        return integratedSegmentCount > 0
            ? new IntervalEnergyNodeResult(true, totalEnergyKwh, IntervalEnergyBasisKind.IntegratedPower, null, null)
            : new IntervalEnergyNodeResult(false, 0d, null, IntervalEnergyBasisFailureKind.CannotIntegrateSafely, "The active power series does not contain enough ordered samples to integrate energy safely over the selected interval.");
    }

    private async Task<IntervalEnergyNodeResult> LoadIntervalEnergyFromCounterSourceAsync(
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var samples = await LoadRawSamplesWithContextAsync(filePath, source.ColumnName, from, to, ct);
        var energyToKwhFactor = ResolveEnergyToKilowattHourFactor(source.Unit);
        var totalEnergyKwh = 0d;
        var integratedSegmentCount = 0;

        for (var index = 0; index < samples.Count - 1; index++)
        {
            var current = samples[index];
            var next = samples[index + 1];
            if (next.TimestampUtc <= current.TimestampUtc)
            {
                continue;
            }

            var delta = next.Value - current.Value;
            if (!double.IsFinite(delta) || delta < 0d)
            {
                continue;
            }

            var segmentStart = MaxDateTime(current.TimestampUtc, from);
            var segmentEnd = MinDateTime(next.TimestampUtc, to);
            if (segmentEnd <= segmentStart)
            {
                continue;
            }

            var segmentDurationHours = (next.TimestampUtc - current.TimestampUtc).TotalHours;
            if (segmentDurationHours <= 0d)
            {
                continue;
            }

            totalEnergyKwh += delta * (segmentEnd - segmentStart).TotalHours / segmentDurationHours * energyToKwhFactor;
            integratedSegmentCount++;
        }

        return integratedSegmentCount > 0
            ? new IntervalEnergyNodeResult(true, totalEnergyKwh, IntervalEnergyBasisKind.CumulativeEnergyCounter, null, null)
            : new IntervalEnergyNodeResult(false, 0d, null, IntervalEnergyBasisFailureKind.CannotIntegrateSafely, "The active cumulative energy series does not contain enough ordered counter samples to derive interval energy safely.");
    }

    private async Task<IntervalEnergyNodeResult> LoadIntervalEnergyFromDirectEnergySourceAsync(
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        try
        {
            using var reader = OpenCsvReader(filePath);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(headerLine))
            {
                return new IntervalEnergyNodeResult(false, 0d, null, IntervalEnergyBasisFailureKind.MissingUsableEnergyBasis, "The active energy series is empty in the selected interval.");
            }

            if (!TryResolveCsvColumns(headerLine, source.ColumnName, out var timeColIndex, out var valueColIndex))
            {
                return new IntervalEnergyNodeResult(false, 0d, null, IntervalEnergyBasisFailureKind.MissingUsableEnergyBasis, "The active energy series does not expose the expected value column.");
            }

            var energyToKwhFactor = ResolveEnergyToKilowattHourFactor(source.Unit);
            var totalEnergyKwh = 0d;
            var sampleCount = 0;
            string? line;

            while ((line = await reader.ReadLineAsync(ct)) is not null)
            {
                var cols = line.Split(',');
                if (cols.Length <= Math.Max(timeColIndex, valueColIndex))
                {
                    continue;
                }

                if (!TryParseTimestamp(cols[timeColIndex], out var timestamp) || timestamp < from || timestamp >= to)
                {
                    continue;
                }

                if (!double.TryParse(cols[valueColIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                    || !double.IsFinite(value))
                {
                    continue;
                }

                totalEnergyKwh += value * energyToKwhFactor;
                sampleCount++;
            }

            return sampleCount > 0
                ? new IntervalEnergyNodeResult(true, totalEnergyKwh, IntervalEnergyBasisKind.DirectEnergy, null, null)
                : new IntervalEnergyNodeResult(false, 0d, null, IntervalEnergyBasisFailureKind.MissingUsableEnergyBasis, "The active energy series has no usable samples in the selected interval.");
        }
        catch
        {
            return new IntervalEnergyNodeResult(false, 0d, null, IntervalEnergyBasisFailureKind.MissingUsableEnergyBasis, "The active energy series could not be read for the selected interval.");
        }
    }

    private static string DescribeIntervalEnergyBasis(IReadOnlyCollection<IntervalEnergyBasisKind> basisKinds)
    {
        if (basisKinds.Count == 0)
        {
            return "a usable energy basis";
        }

        if (basisKinds.Count == 1)
        {
            return basisKinds.First() switch
            {
                IntervalEnergyBasisKind.DirectEnergy => "direct energy samples normalized to kWh",
                IntervalEnergyBasisKind.CumulativeEnergyCounter => "cumulative energy counters converted to interval energy in kWh",
                IntervalEnergyBasisKind.IntegratedPower => "power integrated over actual timestamp spacing into kWh",
                _ => "a usable energy basis"
            };
        }

        return "a mixed usable energy basis normalized to kWh without switching signals";
    }

    private static string JoinEvaluationBasis(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left))
        {
            return right;
        }

        if (string.IsNullOrWhiteSpace(right))
        {
            return left;
        }

        return $"{left} {right}";
    }

    private static bool IsAreaNodeType(string? nodeType)
        => string.Equals(nodeType?.Trim(), FacilityBuiltInNodeTypes.AreaNodeType, StringComparison.OrdinalIgnoreCase);

    private static string? ResolveEffectiveNodeType(
        string nodeKey,
        IReadOnlyDictionary<string, DiplomovaPrace.Persistence.Schematic.SchematicNodeEntity> facilityNodesByKey,
        IReadOnlyDictionary<string, FacilityNodeEditorState> nodeStates)
    {
        if (nodeStates.TryGetValue(nodeKey, out var nodeState) && !string.IsNullOrWhiteSpace(nodeState.NodeType))
        {
            return nodeState.NodeType;
        }

        return facilityNodesByKey.TryGetValue(nodeKey, out var facilityNode)
            ? facilityNode.NodeType
            : null;
    }

    private async Task<SelectionTemperatureLoadScatterResult> BuildSelectionTemperatureLoadScatterAsync(
        SelectionSignalAvailabilityItem? selectionSignal,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        if (selectionSignal is null)
        {
            return new SelectionTemperatureLoadScatterResult
            {
                Summary = "Temperature vs load scatter waits for an active exact signal code.",
                Methodology = TemperatureLoadScatterMethodology
            };
        }

        if (selectionSignal.SignalFamily is not (FacilitySignalFamily.Energy or FacilitySignalFamily.Power))
        {
            return new SelectionTemperatureLoadScatterResult
            {
                IsApplicable = false,
                SignalCode = selectionSignal.ExactSignalCode.Value,
                Summary = $"Temperature vs load scatter requires an energy or power signal. The active signal {selectionSignal.ExactSignalCode.Value} belongs to {selectionSignal.SignalFamily}.",
                Methodology = TemperatureLoadScatterMethodology
            };
        }

        if (!selectionSignal.CanAggregate && !selectionSignal.CanRenderSingleNodeSeries)
        {
            return new SelectionTemperatureLoadScatterResult
            {
                IsApplicable = true,
                SignalCode = selectionSignal.ExactSignalCode.Value,
                Summary = "Temperature vs load scatter is unavailable for the active signal in the current scope.",
                Methodology = TemperatureLoadScatterMethodology,
                Message = selectionSignal.AvailabilityMessage,
                Unit = selectionSignal.SignalFamily == FacilitySignalFamily.Power
                    ? (string.IsNullOrWhiteSpace(selectionSignal.Unit) ? "kW" : selectionSignal.Unit)
                    : ResolveEnergyDerivedLoadUnit(selectionSignal.Unit)
            };
        }

        var pairingHours = GetCompleteUtcHourStarts(from, to);
        if (pairingHours.Count == 0)
        {
            return new SelectionTemperatureLoadScatterResult
            {
                IsApplicable = true,
                SignalCode = selectionSignal.ExactSignalCode.Value,
                Summary = "Temperature vs load scatter needs at least one complete UTC hour in the selected interval.",
                Methodology = TemperatureLoadScatterMethodology,
                Message = "The scatter uses explicit hourly pairing, so the selected interval must contain at least one full UTC hour."
            };
        }

        var facility = await _facilityQueryService.GetMainFacilityAsync(ct);
        var weatherResolution = _weatherSourceResolver.Resolve(facility);
        if (weatherResolution?.HasTemperatureBinding != true)
        {
            return new SelectionTemperatureLoadScatterResult
            {
                IsApplicable = true,
                SignalCode = selectionSignal.ExactSignalCode.Value,
                Summary = "Temperature vs load scatter is unavailable because no facility weather Ta source is resolved.",
                Methodology = TemperatureLoadScatterMethodology,
                Message = "The current facility does not expose a usable Ta weather binding for hourly pairing."
            };
        }

        var scatterWindowStart = pairingHours[0];
        var scatterWindowEndExclusive = pairingHours[^1].AddHours(1);

        var loadSeries = await BuildSelectionHourlyLoadScatterSeriesAsync(selectionSignal, scatterWindowStart, scatterWindowEndExclusive, ct);
        if (loadSeries is null)
        {
            var unit = selectionSignal.SignalFamily == FacilitySignalFamily.Power
                ? (string.IsNullOrWhiteSpace(selectionSignal.Unit) ? "kW" : selectionSignal.Unit)
                : ResolveEnergyDerivedLoadUnit(selectionSignal.Unit);

            return new SelectionTemperatureLoadScatterResult
            {
                IsApplicable = true,
                SignalCode = selectionSignal.ExactSignalCode.Value,
                Unit = unit,
                UsesEnergyDerivedLoad = selectionSignal.SignalFamily == FacilitySignalFamily.Energy,
                Summary = "Temperature vs load scatter is unavailable because a valid hourly load basis could not be prepared safely.",
                Methodology = TemperatureLoadScatterMethodology,
                Message = selectionSignal.SignalFamily == FacilitySignalFamily.Power
                    ? "The active power signal does not provide enough hourly load values in the selected interval."
                    : "The active energy signal does not provide a safe hourly energy-derived load basis in the selected interval."
            };
        }

        if (IsMixedSignAggregateSelectionPowerSeries(selectionSignal, loadSeries.LoadByHour.Values.ToArray()))
        {
            return new SelectionTemperatureLoadScatterResult
            {
                IsApplicable = true,
                SignalCode = selectionSignal.ExactSignalCode.Value,
                Unit = loadSeries.Unit,
                YAxisLabel = loadSeries.YAxisLabel,
                LoadBasisLabel = loadSeries.LoadBasisLabel,
                UsesEnergyDerivedLoad = loadSeries.UsesEnergyDerivedLoad,
                Summary = "Temperature vs load scatter is unavailable because the active aggregate P load basis is mixed-sign.",
                Methodology = TemperatureLoadScatterMethodology,
                Message = "Weather-aware scatter requires a consumption-oriented, non-mixed-sign load basis. Choose a non-mixed-sign signal or narrower scope instead of silently switching signals.",
                EvaluationBasis = $"{loadSeries.EvaluationBasis} Current aggregate P contains both positive and negative hourly load values."
            };
        }

        var temperatureSeries = await BuildHourlyTemperatureScatterSeriesAsync(weatherResolution, scatterWindowStart, scatterWindowEndExclusive, ct);
        if (temperatureSeries is null)
        {
            return new SelectionTemperatureLoadScatterResult
            {
                IsApplicable = true,
                SignalCode = selectionSignal.ExactSignalCode.Value,
                Unit = loadSeries.Unit,
                YAxisLabel = loadSeries.YAxisLabel,
                LoadBasisLabel = loadSeries.LoadBasisLabel,
                UsesEnergyDerivedLoad = loadSeries.UsesEnergyDerivedLoad,
                Summary = "Temperature vs load scatter is unavailable because facility Ta could not be prepared as an hourly weather input.",
                Methodology = TemperatureLoadScatterMethodology,
                Message = "The resolved facility weather source does not provide enough hourly Ta coverage for the selected interval.",
                EvaluationBasis = loadSeries.EvaluationBasis
            };
        }

        var points = pairingHours
            .Where(hour => loadSeries.LoadByHour.ContainsKey(hour) && temperatureSeries.TemperatureByHour.ContainsKey(hour))
            .Select(hour => new SelectionTemperatureLoadScatterPoint
            {
                TimestampUtc = hour,
                OutdoorTemperatureC = temperatureSeries.TemperatureByHour[hour],
                LoadValue = loadSeries.LoadByHour[hour]
            })
            .ToList();

        if (points.Count == 0)
        {
            return new SelectionTemperatureLoadScatterResult
            {
                IsApplicable = true,
                SignalCode = selectionSignal.ExactSignalCode.Value,
                Unit = loadSeries.Unit,
                YAxisLabel = loadSeries.YAxisLabel,
                LoadBasisLabel = loadSeries.LoadBasisLabel,
                UsesEnergyDerivedLoad = loadSeries.UsesEnergyDerivedLoad,
                Summary = "Temperature vs load scatter is unavailable because no complete-hour Ta/load pairs overlap in the selected interval.",
                Methodology = TemperatureLoadScatterMethodology,
                Message = $"Usable hourly load basis was prepared for {loadSeries.LoadByHour.Count} complete hours and facility Ta for {temperatureSeries.TemperatureByHour.Count} complete hours.",
                EvaluationBasis = loadSeries.EvaluationBasis
            };
        }

        var weatherBasis = string.IsNullOrWhiteSpace(temperatureSeries.EvaluationBasis)
            ? string.Empty
            : $" {temperatureSeries.EvaluationBasis}";

        return new SelectionTemperatureLoadScatterResult
        {
            IsApplicable = true,
            IsAvailable = true,
            UsesEnergyDerivedLoad = loadSeries.UsesEnergyDerivedLoad,
            PointCount = points.Count,
            ContributingNodeCount = loadSeries.ContributingNodeKeys.Count,
            SignalCode = selectionSignal.ExactSignalCode.Value,
            Unit = loadSeries.Unit,
            GranularityLabel = "Hourly pairing",
            XAxisLabel = "Outdoor temperature Ta (C)",
            YAxisLabel = loadSeries.YAxisLabel,
            LoadBasisLabel = loadSeries.LoadBasisLabel,
            EvaluationBasis = $"{loadSeries.EvaluationBasis}{weatherBasis}".Trim(),
            Summary = $"Temperature vs load scatter pairs facility Ta with {points.Count} hourly points from the active {selectionSignal.ExactSignalCode.Value} signal.",
            Methodology = TemperatureLoadScatterMethodology,
            WeatherNodeKey = temperatureSeries.WeatherNodeKey,
            Points = points
        };
    }

    private async Task<HourlyLoadScatterSeries?> BuildSelectionHourlyLoadScatterSeriesAsync(
        SelectionSignalAvailabilityItem selectionSignal,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var candidateNodeResults = new List<(string NodeKey, IReadOnlyDictionary<DateTime, double> LoadByHour)>();

        foreach (var nodeKey in selectionSignal.MatchingNodeKeys)
        {
            var source = ResolveCuratedNodeSource(nodeKey, selectionSignal.ExactSignalCode);
            if (source is null)
            {
                continue;
            }

            var filePath = ResolveCuratedFilePath(source);
            if (filePath is null)
            {
                continue;
            }

            var hourlyLoad = await LoadHourlyLoadBasisForSourceAsync(filePath, source, from, to, ct);
            if (hourlyLoad.Count == 0)
            {
                continue;
            }

            candidateNodeResults.Add((nodeKey, hourlyLoad));
        }

        if (candidateNodeResults.Count == 0)
        {
            return null;
        }

        var aggregateLoadByHour = new Dictionary<DateTime, double>();
        foreach (var result in candidateNodeResults)
        {
            foreach (var (hourStart, value) in result.LoadByHour)
            {
                if (aggregateLoadByHour.TryGetValue(hourStart, out var existingValue))
                {
                    aggregateLoadByHour[hourStart] = existingValue + value;
                }
                else
                {
                    aggregateLoadByHour[hourStart] = value;
                }
            }
        }

        if (aggregateLoadByHour.Count == 0)
        {
            return null;
        }

        var usesEnergyDerivedLoad = selectionSignal.SignalFamily == FacilitySignalFamily.Energy;
        var unit = usesEnergyDerivedLoad
            ? ResolveEnergyDerivedLoadUnit(selectionSignal.Unit)
            : (string.IsNullOrWhiteSpace(selectionSignal.Unit) ? "kW" : selectionSignal.Unit);
        var yAxisLabel = string.IsNullOrWhiteSpace(unit)
            ? (usesEnergyDerivedLoad ? "Energy-derived load" : "Load")
            : $"{(usesEnergyDerivedLoad ? "Energy-derived load" : "Load")} ({unit})";
        var loadBasisLabel = usesEnergyDerivedLoad
            ? "hourly energy-derived load"
            : "hourly average power";
        var evaluationBasis = selectionSignal.CanAggregate
            ? $"Evaluation basis: {loadBasisLabel} aggregated across {candidateNodeResults.Count} of {selectionSignal.MatchingNodeCount} matching nodes for {selectionSignal.ExactSignalCode.Value}; each paired hour sums nodes with a valid load basis in that hour."
            : $"Evaluation basis: {loadBasisLabel} from the active {selectionSignal.ExactSignalCode.Value} signal on the current scope node.";

        return new HourlyLoadScatterSeries(
            aggregateLoadByHour,
            candidateNodeResults.Select(result => result.NodeKey).ToList(),
            selectionSignal.MatchingNodeCount,
            unit,
            yAxisLabel,
            loadBasisLabel,
            evaluationBasis,
            usesEnergyDerivedLoad);
    }

    private async Task<HourlyTemperatureScatterSeries?> BuildHourlyTemperatureScatterSeriesAsync(
        FacilityWeatherSourceResolution weatherResolution,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        if (weatherResolution.TemperatureBinding is null)
        {
            return null;
        }

        var weatherSource = BuildBindingCuratedNodeSource(weatherResolution.NodeKey, weatherResolution.TemperatureBinding);
        var weatherFilePath = ResolveCuratedFilePath(weatherSource);
        if (weatherFilePath is null)
        {
            return null;
        }

        var hourlyWeather = await LoadHourlyAverageTemperatureForSourceAsync(weatherFilePath, weatherSource, from, to, ct);
        if (hourlyWeather.Count == 0)
        {
            return null;
        }

        var basis = $"Weather input: hourly average facility Ta from weather node {weatherResolution.NodeKey}.";
        return new HourlyTemperatureScatterSeries(hourlyWeather, weatherResolution.NodeKey, basis);
    }

    private async Task<IReadOnlyDictionary<DateTime, double>> LoadHourlyLoadBasisForSourceAsync(
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        if (source.IsPowerSignal)
        {
            return await LoadHourlyAveragePowerForSourceAsync(filePath, source, from, to, ct);
        }

        if (source.SeriesSemantics == FacilitySignalSeriesSemantics.CumulativeCounter)
        {
            return await LoadHourlyAverageLoadFromCounterSourceAsync(filePath, source, from, to, ct);
        }

        return await LoadHourlyAverageLoadFromDirectEnergySourceAsync(filePath, source, from, to, ct);
    }

    private async Task<IReadOnlyDictionary<DateTime, double>> LoadHourlyAveragePowerForSourceAsync(
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var samples = await LoadRawSamplesWithContextAsync(filePath, source.ColumnName, from, to, ct);
        var weightedLoadByHour = InitializeHourlyAccumulatorMap(from, to);
        var coverageByHour = InitializeHourlyAccumulatorMap(from, to);

        for (var index = 0; index < samples.Count - 1; index++)
        {
            var current = samples[index];
            var next = samples[index + 1];
            if (next.TimestampUtc <= current.TimestampUtc)
            {
                continue;
            }

            var segmentStart = MaxDateTime(current.TimestampUtc, from);
            var segmentEnd = MinDateTime(next.TimestampUtc, to);
            if (segmentEnd <= segmentStart)
            {
                continue;
            }

            var powerKw = NormalizeValueForStats(current.Value, source);
            if (!double.IsFinite(powerKw))
            {
                continue;
            }

            AccumulateSegmentByHour(
                segmentStart,
                segmentEnd,
                overlapHours => powerKw * overlapHours,
                weightedLoadByHour,
                coverageByHour);
        }

        return FinalizeHourlyAverageMap(weightedLoadByHour, coverageByHour, TemperatureLoadScatterMinimumHourlyCoverageRatio);
    }

    private async Task<IReadOnlyDictionary<DateTime, double>> LoadHourlyAverageLoadFromCounterSourceAsync(
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var samples = await LoadRawSamplesWithContextAsync(filePath, source.ColumnName, from, to, ct);
        var weightedLoadByHour = InitializeHourlyAccumulatorMap(from, to);
        var coverageByHour = InitializeHourlyAccumulatorMap(from, to);

        for (var index = 0; index < samples.Count - 1; index++)
        {
            var current = samples[index];
            var next = samples[index + 1];
            if (next.TimestampUtc <= current.TimestampUtc)
            {
                continue;
            }

            var delta = next.Value - current.Value;
            if (!double.IsFinite(delta) || delta < 0)
            {
                continue;
            }

            var segmentStart = MaxDateTime(current.TimestampUtc, from);
            var segmentEnd = MinDateTime(next.TimestampUtc, to);
            if (segmentEnd <= segmentStart)
            {
                continue;
            }

            var segmentDurationHours = (next.TimestampUtc - current.TimestampUtc).TotalHours;
            if (segmentDurationHours <= 0)
            {
                continue;
            }

            var averageLoad = delta / segmentDurationHours;
            if (!double.IsFinite(averageLoad))
            {
                continue;
            }

            AccumulateSegmentByHour(
                segmentStart,
                segmentEnd,
                overlapHours => averageLoad * overlapHours,
                weightedLoadByHour,
                coverageByHour);
        }

        return FinalizeHourlyAverageMap(weightedLoadByHour, coverageByHour, TemperatureLoadScatterMinimumHourlyCoverageRatio);
    }

    private async Task<IReadOnlyDictionary<DateTime, double>> LoadHourlyAverageLoadFromDirectEnergySourceAsync(
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var samples = await LoadRawSamplesWithContextAsync(filePath, source.ColumnName, from, to, ct);
        var weightedLoadByHour = InitializeHourlyAccumulatorMap(from, to);
        var coverageByHour = InitializeHourlyAccumulatorMap(from, to);

        for (var index = 0; index < samples.Count - 1; index++)
        {
            var current = samples[index];
            var next = samples[index + 1];
            if (next.TimestampUtc <= current.TimestampUtc)
            {
                continue;
            }

            var segmentStart = MaxDateTime(current.TimestampUtc, from);
            var segmentEnd = MinDateTime(next.TimestampUtc, to);
            if (segmentEnd <= segmentStart)
            {
                continue;
            }

            var segmentDurationHours = (next.TimestampUtc - current.TimestampUtc).TotalHours;
            if (segmentDurationHours <= 0)
            {
                continue;
            }

            var intervalEnergy = NormalizeValueForStats(current.Value, source);
            if (!double.IsFinite(intervalEnergy))
            {
                continue;
            }

            var averageLoad = intervalEnergy / segmentDurationHours;
            if (!double.IsFinite(averageLoad))
            {
                continue;
            }

            AccumulateSegmentByHour(
                segmentStart,
                segmentEnd,
                overlapHours => averageLoad * overlapHours,
                weightedLoadByHour,
                coverageByHour);
        }

        return FinalizeHourlyAverageMap(weightedLoadByHour, coverageByHour, TemperatureLoadScatterMinimumHourlyCoverageRatio);
    }

    private async Task<IReadOnlyDictionary<DateTime, double>> LoadHourlyAverageTemperatureForSourceAsync(
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var samples = await LoadRawSamplesWithContextAsync(filePath, source.ColumnName, from, to, ct);
        var weightedTemperatureByHour = InitializeHourlyAccumulatorMap(from, to);
        var coverageByHour = InitializeHourlyAccumulatorMap(from, to);

        for (var index = 0; index < samples.Count - 1; index++)
        {
            var current = samples[index];
            var next = samples[index + 1];
            if (next.TimestampUtc <= current.TimestampUtc)
            {
                continue;
            }

            var segmentStart = MaxDateTime(current.TimestampUtc, from);
            var segmentEnd = MinDateTime(next.TimestampUtc, to);
            if (segmentEnd <= segmentStart)
            {
                continue;
            }

            if (!double.IsFinite(current.Value))
            {
                continue;
            }

            AccumulateSegmentByHour(
                segmentStart,
                segmentEnd,
                overlapHours => current.Value * overlapHours,
                weightedTemperatureByHour,
                coverageByHour);
        }

        return FinalizeHourlyAverageMap(weightedTemperatureByHour, coverageByHour, TemperatureLoadScatterMinimumHourlyCoverageRatio);
    }

    private static string ResolveEnergyDerivedLoadUnit(string energyUnit)
    {
        if (string.IsNullOrWhiteSpace(energyUnit))
        {
            return "kW";
        }

        return energyUnit.Trim().ToLowerInvariant() switch
        {
            "wh" => "W",
            "kwh" => "kW",
            "mwh" => "MW",
            "gwh" => "GW",
            _ => $"{energyUnit}/h"
        };
    }

    private async Task<SelectionWeatherAwareBaselineResult> BuildSelectionWeatherAwareBaselineAsync(
        SelectionSignalAvailabilityItem? selectionSignal,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        if (selectionSignal is null)
        {
            return new SelectionWeatherAwareBaselineResult
            {
                Summary = "Weather-aware baseline waits for an active exact signal code.",
                Methodology = WeatherAwareBaselineMethodology
            };
        }

        if (selectionSignal.SignalFamily is not (FacilitySignalFamily.Energy or FacilitySignalFamily.Power))
        {
            return new SelectionWeatherAwareBaselineResult
            {
                IsApplicable = false,
                Summary = $"Weather-aware baseline requires an energy or power signal. The active signal {selectionSignal.ExactSignalCode.Value} belongs to {selectionSignal.SignalFamily}.",
                Methodology = WeatherAwareBaselineMethodology
            };
        }

        if (!selectionSignal.CanAggregate && !selectionSignal.CanRenderSingleNodeSeries)
        {
            return new SelectionWeatherAwareBaselineResult
            {
                IsApplicable = true,
                Summary = "Weather-aware baseline is unavailable for the active signal in the current scope.",
                Methodology = WeatherAwareBaselineMethodology,
                Message = selectionSignal.AvailabilityMessage
            };
        }

        var predictionDays = GetCompleteUtcDayStarts(from, to);
        if (predictionDays.Count == 0)
        {
            return new SelectionWeatherAwareBaselineResult
            {
                IsApplicable = true,
                Summary = "Weather-aware baseline needs at least one complete UTC day in the selected interval.",
                Methodology = WeatherAwareBaselineMethodology,
                Message = "Daily baseline is computed only from complete UTC days. The current interval does not contain any complete day."
            };
        }

        var fitEndExclusive = predictionDays[0];
        var fitStartInclusive = fitEndExclusive.AddDays(-WeatherAwareBaselineFitLookbackDays);
        var windowEndExclusive = predictionDays[^1].AddDays(1);

        var facility = await _facilityQueryService.GetMainFacilityAsync(ct);
        var weatherResolution = _weatherSourceResolver.Resolve(facility);
        if (weatherResolution?.HasTemperatureBinding != true)
        {
            return new SelectionWeatherAwareBaselineResult
            {
                IsApplicable = true,
                Summary = "Weather-aware baseline is unavailable because no facility weather Ta source is resolved.",
                Methodology = WeatherAwareBaselineMethodology,
                Message = "The current facility does not expose a usable Ta weather binding outside the selected scope."
            };
        }

        var dailyEnergySeries = await BuildWeatherAwareDailyEnergySeriesAsync(
            selectionSignal,
            fitStartInclusive,
            fitEndExclusive,
            predictionDays,
            windowEndExclusive,
            ct);

        if (dailyEnergySeries is null)
        {
            return new SelectionWeatherAwareBaselineResult
            {
                IsApplicable = true,
                Summary = "Weather-aware baseline is unavailable because daily energy could not be prepared safely.",
                Methodology = WeatherAwareBaselineMethodology,
                Message = "The active signal does not provide a usable daily energy series for the selected scope.",
                Unit = selectionSignal.SignalFamily == FacilitySignalFamily.Power
                    ? "kWh"
                    : (string.IsNullOrWhiteSpace(selectionSignal.Unit) ? "kWh" : selectionSignal.Unit)
            };
        }

        var dailyWeatherSeries = await BuildWeatherAwareDailyTemperatureSeriesAsync(
            weatherResolution,
            fitStartInclusive,
            windowEndExclusive,
            ct);

        if (dailyWeatherSeries is null)
        {
            return new SelectionWeatherAwareBaselineResult
            {
                IsApplicable = true,
                Summary = "Weather-aware baseline is unavailable because facility Ta could not be prepared as daily weather input.",
                Methodology = WeatherAwareBaselineMethodology,
                Message = "The resolved facility weather source does not provide enough daily Ta coverage for the fit and evaluation window.",
                Unit = dailyEnergySeries.Unit
            };
        }

        var fitDays = GetCompleteUtcDayStarts(fitStartInclusive, fitEndExclusive);
        var fitObservations = fitDays
            .Where(day => dailyEnergySeries.DailyEnergyByDay.ContainsKey(day) && dailyWeatherSeries.DailyAverageTemperatureByDay.ContainsKey(day))
            .Select(day => new DailyWeatherAwareObservation(
                day,
                dailyEnergySeries.DailyEnergyByDay[day],
                dailyWeatherSeries.DailyAverageTemperatureByDay[day]))
            .ToList();

        if (fitObservations.Count < WeatherAwareBaselineMinimumFitDays)
        {
            return new SelectionWeatherAwareBaselineResult
            {
                IsApplicable = true,
                Summary = "Weather-aware baseline is unavailable because the fit period does not contain enough valid daily points.",
                Methodology = WeatherAwareBaselineMethodology,
                Message = $"Only {fitObservations.Count} fit days contain both daily energy and facility Ta. At least {WeatherAwareBaselineMinimumFitDays} valid fit days are required.",
                Unit = dailyEnergySeries.Unit,
                EvaluationBasis = dailyEnergySeries.EvaluationBasis
            };
        }

        if (!TryFitWeatherAwareBaselineModel(fitObservations, out var model))
        {
            return new SelectionWeatherAwareBaselineResult
            {
                IsApplicable = true,
                Summary = "Weather-aware baseline is unavailable because the daily HDD/CDD model is numerically unstable for the current fit period.",
                Methodology = WeatherAwareBaselineMethodology,
                Message = "The fit-period daily points do not span a stable enough temperature-response pattern for a 3-parameter HDD/CDD regression.",
                Unit = dailyEnergySeries.Unit,
                EvaluationBasis = dailyEnergySeries.EvaluationBasis
            };
        }

        if (!TryComputeWeatherAwareBaselineDiagnostics(fitObservations, model, out var cvRmsePercent, out var nmbePercent))
        {
            return new SelectionWeatherAwareBaselineResult
            {
                IsApplicable = true,
                Summary = "Weather-aware baseline is unavailable because CV(RMSE) and NMBE cannot be computed safely for the fit period.",
                Methodology = WeatherAwareBaselineMethodology,
                Message = "The fit period does not support stable diagnostics because the average daily energy is too small or there are too few degrees of freedom.",
                Unit = dailyEnergySeries.Unit,
                EvaluationBasis = dailyEnergySeries.EvaluationBasis
            };
        }

        var predictionObservations = predictionDays
            .Where(day => dailyEnergySeries.DailyEnergyByDay.ContainsKey(day) && dailyWeatherSeries.DailyAverageTemperatureByDay.ContainsKey(day))
            .Select(day => new DailyWeatherAwareObservation(
                day,
                dailyEnergySeries.DailyEnergyByDay[day],
                dailyWeatherSeries.DailyAverageTemperatureByDay[day]))
            .ToList();

        if (predictionObservations.Count != predictionDays.Count)
        {
            return new SelectionWeatherAwareBaselineResult
            {
                IsApplicable = true,
                Summary = "Weather-aware baseline is unavailable because the selected interval does not have full daily energy and Ta coverage for every complete day.",
                Methodology = WeatherAwareBaselineMethodology,
                Message = $"Usable daily baseline inputs were prepared for {predictionObservations.Count} of {predictionDays.Count} complete days in the selected interval.",
                Unit = dailyEnergySeries.Unit,
                EvaluationBasis = dailyEnergySeries.EvaluationBasis
            };
        }

        var actualValue = predictionObservations.Sum(observation => observation.Energy);
        var baselineExpectedValue = predictionObservations.Sum(observation => model.Predict(observation.AverageOutdoorTemperatureC));
        var minimumMeaningfulBaseline = Math.Max(1.0, predictionObservations.Count * 0.5);
        if (Math.Abs(baselineExpectedValue) < minimumMeaningfulBaseline)
        {
            return new SelectionWeatherAwareBaselineResult
            {
                IsApplicable = true,
                Summary = "Weather-aware baseline is unavailable because the modelled daily baseline is too small for stable percentage interpretation.",
                Methodology = WeatherAwareBaselineMethodology,
                Message = "The baseline-expected energy over the selected full days is too close to zero for a meaningful delta-percent evaluation.",
                Unit = dailyEnergySeries.Unit,
                EvaluationBasis = dailyEnergySeries.EvaluationBasis
            };
        }

        var deltaAbsolute = actualValue - baselineExpectedValue;
        var deltaPercent = (deltaAbsolute / baselineExpectedValue) * 100.0;
        var weatherBasis = string.IsNullOrWhiteSpace(dailyWeatherSeries.EvaluationBasis)
            ? string.Empty
            : $" {dailyWeatherSeries.EvaluationBasis}";

        return new SelectionWeatherAwareBaselineResult
        {
            IsApplicable = true,
            IsAvailable = true,
            Severity = ClassifySeverity(deltaPercent),
            ActualValue = actualValue,
            BaselineExpectedValue = baselineExpectedValue,
            DeltaAbsolute = deltaAbsolute,
            DeltaPercent = deltaPercent,
            CvRmsePercent = cvRmsePercent,
            NmbePercent = nmbePercent,
            FitDayCount = fitObservations.Count,
            PredictionDayCount = predictionObservations.Count,
            ContributingNodeCount = dailyEnergySeries.ContributingNodeKeys.Count,
            Unit = dailyEnergySeries.Unit,
            EvaluationBasis = $"{dailyEnergySeries.EvaluationBasis}{weatherBasis}".Trim(),
            Summary = $"Weather-aware baseline uses {fitObservations.Count} fit days and {predictionObservations.Count} selected full days from the active {selectionSignal.ExactSignalCode.Value} signal.",
            Methodology = WeatherAwareBaselineMethodology,
            WeatherNodeKey = dailyWeatherSeries.WeatherNodeKey
        };
    }

    private async Task<WeatherAwareDailyEnergySeries?> BuildWeatherAwareDailyEnergySeriesAsync(
        SelectionSignalAvailabilityItem selectionSignal,
        DateTime fitStartInclusive,
        DateTime fitEndExclusive,
        IReadOnlyList<DateTime> predictionDays,
        DateTime windowEndExclusive,
        CancellationToken ct)
    {
        var candidateNodeResults = new List<(string NodeKey, IReadOnlyDictionary<DateTime, double> DailyEnergyByDay)>();

        foreach (var nodeKey in selectionSignal.MatchingNodeKeys)
        {
            var source = ResolveCuratedNodeSource(nodeKey, selectionSignal.ExactSignalCode);
            if (source is null)
            {
                continue;
            }

            var filePath = ResolveCuratedFilePath(source);
            if (filePath is null)
            {
                continue;
            }

            var nodeDailyEnergy = await LoadDailyEnergyForSourceAsync(filePath, source, fitStartInclusive, windowEndExclusive, ct);
            if (nodeDailyEnergy.Count == 0)
            {
                continue;
            }

            candidateNodeResults.Add((nodeKey, nodeDailyEnergy));
        }

        if (candidateNodeResults.Count == 0)
        {
            return null;
        }

        var contributingNodeResults = candidateNodeResults
            .Where(result => predictionDays.All(day => result.DailyEnergyByDay.ContainsKey(day)))
            .ToList();

        if (contributingNodeResults.Count == 0)
        {
            return null;
        }

        var aggregateDays = GetCompleteUtcDayStarts(fitStartInclusive, windowEndExclusive);
        var aggregateDailyEnergy = aggregateDays
            .Where(day => contributingNodeResults.All(result => result.DailyEnergyByDay.ContainsKey(day)))
            .ToDictionary(
                day => day,
                day => contributingNodeResults.Sum(result => result.DailyEnergyByDay[day]));

        if (aggregateDailyEnergy.Count == 0)
        {
            return null;
        }

        var evaluationScope = selectionSignal.CanAggregate
            ? $"Evaluation basis: daily energy aggregated across {contributingNodeResults.Count} of {selectionSignal.MatchingNodeCount} matching nodes for {selectionSignal.ExactSignalCode.Value}."
            : $"Evaluation basis: daily energy from the active exact signal {selectionSignal.ExactSignalCode.Value} on the current scope node.";

        var unit = selectionSignal.SignalFamily == FacilitySignalFamily.Power
            ? "kWh"
            : string.IsNullOrWhiteSpace(selectionSignal.Unit)
                ? "kWh"
                : selectionSignal.Unit;

        return new WeatherAwareDailyEnergySeries(
            aggregateDailyEnergy,
            contributingNodeResults.Select(result => result.NodeKey).ToList(),
            selectionSignal.MatchingNodeCount,
            unit,
            evaluationScope);
    }

    private async Task<WeatherAwareDailyTemperatureSeries?> BuildWeatherAwareDailyTemperatureSeriesAsync(
        FacilityWeatherSourceResolution weatherResolution,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        if (weatherResolution.TemperatureBinding is null)
        {
            return null;
        }

        var weatherSource = BuildBindingCuratedNodeSource(weatherResolution.NodeKey, weatherResolution.TemperatureBinding);
        var weatherFilePath = ResolveCuratedFilePath(weatherSource);
        if (weatherFilePath is null)
        {
            return null;
        }

        var dailyWeather = await LoadDailyAverageTemperatureForSourceAsync(weatherFilePath, weatherSource, from, to, ct);
        if (dailyWeather.Count == 0)
        {
            return null;
        }

        var basis = $"Weather input: facility Ta from weather node {weatherResolution.NodeKey}.";
        return new WeatherAwareDailyTemperatureSeries(dailyWeather, weatherResolution.NodeKey, basis);
    }

    private async Task<IReadOnlyDictionary<DateTime, double>> LoadDailyEnergyForSourceAsync(
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        if (source.SeriesSemantics == FacilitySignalSeriesSemantics.CumulativeCounter)
        {
            return await LoadDailyEnergyFromCounterSourceAsync(filePath, source, from, to, ct);
        }

        if (source.IsPowerSignal)
        {
            return await LoadDailyEnergyFromPowerSourceAsync(filePath, source, from, to, ct);
        }

        return await LoadDailyEnergyFromDirectEnergySourceAsync(filePath, source, from, to, ct);
    }

    private async Task<IReadOnlyDictionary<DateTime, double>> LoadDailyEnergyFromPowerSourceAsync(
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var samples = await LoadRawSamplesWithContextAsync(filePath, source.ColumnName, from, to, ct);
        var energyByDay = InitializeDailyAccumulatorMap(from, to);
        var coverageByDay = InitializeDailyAccumulatorMap(from, to);

        for (var index = 0; index < samples.Count - 1; index++)
        {
            var current = samples[index];
            var next = samples[index + 1];
            if (next.TimestampUtc <= current.TimestampUtc)
            {
                continue;
            }

            var segmentStart = MaxDateTime(current.TimestampUtc, from);
            var segmentEnd = MinDateTime(next.TimestampUtc, to);
            if (segmentEnd <= segmentStart)
            {
                continue;
            }

            var powerKw = NormalizeValueForStats(current.Value, source);
            if (!double.IsFinite(powerKw))
            {
                continue;
            }

            AccumulateSegmentByDay(
                segmentStart,
                segmentEnd,
                overlapHours => powerKw * overlapHours,
                energyByDay,
                coverageByDay);
        }

        return FinalizeDailyValueMap(energyByDay, coverageByDay, 24.0, WeatherAwareBaselineMinimumDailyCoverageRatio);
    }

    private async Task<IReadOnlyDictionary<DateTime, double>> LoadDailyEnergyFromCounterSourceAsync(
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var samples = await LoadRawSamplesWithContextAsync(filePath, source.ColumnName, from, to, ct);
        var energyByDay = InitializeDailyAccumulatorMap(from, to);
        var coverageByDay = InitializeDailyAccumulatorMap(from, to);

        for (var index = 0; index < samples.Count - 1; index++)
        {
            var current = samples[index];
            var next = samples[index + 1];
            if (next.TimestampUtc <= current.TimestampUtc)
            {
                continue;
            }

            var delta = next.Value - current.Value;
            if (!double.IsFinite(delta) || delta < 0)
            {
                continue;
            }

            var segmentStart = MaxDateTime(current.TimestampUtc, from);
            var segmentEnd = MinDateTime(next.TimestampUtc, to);
            if (segmentEnd <= segmentStart)
            {
                continue;
            }

            var segmentDurationHours = (next.TimestampUtc - current.TimestampUtc).TotalHours;
            if (segmentDurationHours <= 0)
            {
                continue;
            }

            AccumulateSegmentByDay(
                segmentStart,
                segmentEnd,
                overlapHours => delta * (overlapHours / segmentDurationHours),
                energyByDay,
                coverageByDay);
        }

        return FinalizeDailyValueMap(energyByDay, coverageByDay, 24.0, WeatherAwareBaselineMinimumDailyCoverageRatio);
    }

    private async Task<IReadOnlyDictionary<DateTime, double>> LoadDailyEnergyFromDirectEnergySourceAsync(
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        try
        {
            using var reader = OpenCsvReader(filePath);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(headerLine))
            {
                return new Dictionary<DateTime, double>();
            }

            if (!TryResolveCsvColumns(headerLine, source.ColumnName, out var timeColIndex, out var valueColIndex))
            {
                return new Dictionary<DateTime, double>();
            }

            var energyByDay = InitializeDailyAccumulatorMap(from, to);
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) is not null)
            {
                var cols = line.Split(',');
                if (cols.Length <= Math.Max(timeColIndex, valueColIndex))
                {
                    continue;
                }

                if (!TryParseTimestamp(cols[timeColIndex], out var timestamp))
                {
                    continue;
                }

                if (timestamp < from || timestamp >= to)
                {
                    continue;
                }

                if (!double.TryParse(cols[valueColIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                    || !double.IsFinite(value))
                {
                    continue;
                }

                var dayStart = DateTime.SpecifyKind(timestamp.Date, DateTimeKind.Utc);
                if (energyByDay.TryGetValue(dayStart, out var existing))
                {
                    energyByDay[dayStart] = existing + value;
                }
                else
                {
                    energyByDay[dayStart] = value;
                }
            }

            return energyByDay;
        }
        catch
        {
            return new Dictionary<DateTime, double>();
        }
    }

    private async Task<IReadOnlyDictionary<DateTime, double>> LoadDailyAverageTemperatureForSourceAsync(
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var samples = await LoadRawSamplesWithContextAsync(filePath, source.ColumnName, from, to, ct);
        var weightedTemperatureByDay = InitializeDailyAccumulatorMap(from, to);
        var coverageByDay = InitializeDailyAccumulatorMap(from, to);

        for (var index = 0; index < samples.Count - 1; index++)
        {
            var current = samples[index];
            var next = samples[index + 1];
            if (next.TimestampUtc <= current.TimestampUtc)
            {
                continue;
            }

            var segmentStart = MaxDateTime(current.TimestampUtc, from);
            var segmentEnd = MinDateTime(next.TimestampUtc, to);
            if (segmentEnd <= segmentStart)
            {
                continue;
            }

            if (!double.IsFinite(current.Value))
            {
                continue;
            }

            AccumulateSegmentByDay(
                segmentStart,
                segmentEnd,
                overlapHours => current.Value * overlapHours,
                weightedTemperatureByDay,
                coverageByDay);
        }

        var result = new Dictionary<DateTime, double>();
        foreach (var (dayStart, weightedTemperatureHours) in weightedTemperatureByDay)
        {
            if (!coverageByDay.TryGetValue(dayStart, out var coveredHours)
                || coveredHours < 24.0 * WeatherAwareBaselineMinimumDailyCoverageRatio)
            {
                continue;
            }

            result[dayStart] = weightedTemperatureHours / coveredHours;
        }

        return result;
    }

    private async Task<List<(DateTime TimestampUtc, double Value)>> LoadRawSamplesWithContextAsync(
        string filePath,
        string columnName,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var samples = new List<(DateTime TimestampUtc, double Value)>();

        try
        {
            using var reader = OpenCsvReader(filePath);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(headerLine))
            {
                return samples;
            }

            if (!TryResolveCsvColumns(headerLine, columnName, out var timeColIndex, out var valueColIndex))
            {
                return samples;
            }

            (DateTime TimestampUtc, double Value)? previousSample = null;
            var addedContextSample = false;
            string? line;

            while ((line = await reader.ReadLineAsync(ct)) is not null)
            {
                var cols = line.Split(',');
                if (cols.Length <= Math.Max(timeColIndex, valueColIndex))
                {
                    continue;
                }

                if (!TryParseTimestamp(cols[timeColIndex], out var timestamp))
                {
                    continue;
                }

                if (!double.TryParse(cols[valueColIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                    || !double.IsFinite(value))
                {
                    continue;
                }

                if (timestamp < from)
                {
                    previousSample = (timestamp, value);
                    continue;
                }

                if (!addedContextSample && previousSample.HasValue)
                {
                    samples.Add(previousSample.Value);
                    addedContextSample = true;
                }

                samples.Add((timestamp, value));

                if (timestamp > to)
                {
                    break;
                }
            }
        }
        catch
        {
            return new List<(DateTime TimestampUtc, double Value)>();
        }

        return samples;
    }

    private static bool TryResolveCsvColumns(
        string headerLine,
        string valueColumnName,
        out int timeColIndex,
        out int valueColIndex)
    {
        var headers = headerLine.Split(',');
        valueColIndex = Array.FindIndex(headers, header => header.Trim().Equals(valueColumnName, StringComparison.OrdinalIgnoreCase));
        timeColIndex = Array.FindIndex(headers, header =>
            header.Trim().Equals("datetime_utc", StringComparison.OrdinalIgnoreCase) ||
            header.Trim().Equals("timestamp", StringComparison.OrdinalIgnoreCase) ||
            header.Trim().Equals("time", StringComparison.OrdinalIgnoreCase));

        if (timeColIndex == -1)
        {
            timeColIndex = 0;
        }

        return valueColIndex >= 0;
    }

    private static Dictionary<DateTime, double> InitializeHourlyAccumulatorMap(DateTime from, DateTime to)
    {
        return GetCompleteUtcHourStarts(from, to)
            .ToDictionary(hourStart => hourStart, _ => 0d);
    }

    private static IReadOnlyDictionary<DateTime, double> FinalizeHourlyAverageMap(
        IReadOnlyDictionary<DateTime, double> weightedValueByHour,
        IReadOnlyDictionary<DateTime, double> coverageByHour,
        double minimumCoverageRatio)
    {
        var result = new Dictionary<DateTime, double>();

        foreach (var (hourStart, weightedValue) in weightedValueByHour)
        {
            if (!coverageByHour.TryGetValue(hourStart, out var coveredHours) || coveredHours < minimumCoverageRatio)
            {
                continue;
            }

            result[hourStart] = weightedValue / coveredHours;
        }

        return result;
    }

    private static void AccumulateSegmentByHour(
        DateTime segmentStart,
        DateTime segmentEnd,
        Func<double, double> valueFromOverlapHours,
        IDictionary<DateTime, double> valueByHour,
        IDictionary<DateTime, double> coverageByHour)
    {
        var cursor = segmentStart;

        while (cursor < segmentEnd)
        {
            var hourStart = new DateTime(cursor.Year, cursor.Month, cursor.Day, cursor.Hour, 0, 0, DateTimeKind.Utc);
            var hourEnd = hourStart.AddHours(1);
            var chunkEnd = MinDateTime(segmentEnd, hourEnd);
            var overlapHours = (chunkEnd - cursor).TotalHours;
            if (overlapHours > 0 && valueByHour.TryGetValue(hourStart, out var value))
            {
                valueByHour[hourStart] = value + valueFromOverlapHours(overlapHours);
                coverageByHour[hourStart] = coverageByHour[hourStart] + overlapHours;
            }

            cursor = chunkEnd;
        }
    }

    private static IReadOnlyList<DateTime> GetCompleteUtcHourStarts(DateTime from, DateTime to)
    {
        var hours = new List<DateTime>();
        var cursor = new DateTime(from.Year, from.Month, from.Day, from.Hour, 0, 0, DateTimeKind.Utc);
        if (cursor < from)
        {
            cursor = cursor.AddHours(1);
        }

        while (cursor.AddHours(1) <= to)
        {
            hours.Add(cursor);
            cursor = cursor.AddHours(1);
        }

        return hours;
    }

    private static Dictionary<DateTime, double> InitializeDailyAccumulatorMap(DateTime from, DateTime to)
    {
        return GetCompleteUtcDayStarts(from, to)
            .ToDictionary(dayStart => dayStart, _ => 0d);
    }

    private static IReadOnlyDictionary<DateTime, double> FinalizeDailyValueMap(
        IReadOnlyDictionary<DateTime, double> valuesByDay,
        IReadOnlyDictionary<DateTime, double> coverageByDay,
        double fullDayHours,
        double minimumCoverageRatio)
    {
        var result = new Dictionary<DateTime, double>();
        var requiredHours = fullDayHours * minimumCoverageRatio;

        foreach (var (dayStart, value) in valuesByDay)
        {
            if (!coverageByDay.TryGetValue(dayStart, out var coveredHours) || coveredHours < requiredHours)
            {
                continue;
            }

            result[dayStart] = value;
        }

        return result;
    }

    private static void AccumulateSegmentByDay(
        DateTime segmentStart,
        DateTime segmentEnd,
        Func<double, double> valueFromOverlapHours,
        IDictionary<DateTime, double> valueByDay,
        IDictionary<DateTime, double> coverageByDay)
    {
        var cursor = segmentStart;

        while (cursor < segmentEnd)
        {
            var dayStart = DateTime.SpecifyKind(cursor.Date, DateTimeKind.Utc);
            var dayEnd = dayStart.AddDays(1);
            var chunkEnd = MinDateTime(segmentEnd, dayEnd);
            var overlapHours = (chunkEnd - cursor).TotalHours;
            if (overlapHours > 0)
            {
                if (valueByDay.TryGetValue(dayStart, out var value))
                {
                    valueByDay[dayStart] = value + valueFromOverlapHours(overlapHours);
                    coverageByDay[dayStart] = coverageByDay[dayStart] + overlapHours;
                }
            }

            cursor = chunkEnd;
        }
    }

    private static IReadOnlyList<DateTime> GetCompleteUtcDayStarts(DateTime from, DateTime to)
    {
        var days = new List<DateTime>();
        var cursor = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);

        while (cursor < to)
        {
            var dayEnd = cursor.AddDays(1);
            if (cursor >= from && dayEnd <= to)
            {
                days.Add(cursor);
            }

            cursor = dayEnd;
        }

        return days;
    }

    private static bool TryFitWeatherAwareBaselineModel(
        IReadOnlyList<DailyWeatherAwareObservation> observations,
        out WeatherAwareBaselineModel model)
    {
        model = new WeatherAwareBaselineModel(0, 0, 0);
        if (observations.Count < WeatherAwareBaselineMinimumFitDays)
        {
            return false;
        }

        var xtx = new double[3, 3];
        var xty = new double[3];

        foreach (var observation in observations)
        {
            var heating = Math.Max(0, WeatherAwareHeatingBalanceTemperatureC - observation.AverageOutdoorTemperatureC);
            var cooling = Math.Max(0, observation.AverageOutdoorTemperatureC - WeatherAwareCoolingBalanceTemperatureC);
            var x0 = 1d;
            var x1 = heating;
            var x2 = cooling;
            var y = observation.Energy;

            xtx[0, 0] += x0 * x0;
            xtx[0, 1] += x0 * x1;
            xtx[0, 2] += x0 * x2;
            xtx[1, 0] += x1 * x0;
            xtx[1, 1] += x1 * x1;
            xtx[1, 2] += x1 * x2;
            xtx[2, 0] += x2 * x0;
            xtx[2, 1] += x2 * x1;
            xtx[2, 2] += x2 * x2;

            xty[0] += x0 * y;
            xty[1] += x1 * y;
            xty[2] += x2 * y;
        }

        if (!TrySolveLinearSystem3x3(xtx, xty, out var coefficients))
        {
            return false;
        }

        if (coefficients.Any(coefficient => !double.IsFinite(coefficient)))
        {
            return false;
        }

        model = new WeatherAwareBaselineModel(coefficients[0], coefficients[1], coefficients[2]);
        return true;
    }

    private static bool TryComputeWeatherAwareBaselineDiagnostics(
        IReadOnlyList<DailyWeatherAwareObservation> fitObservations,
        WeatherAwareBaselineModel model,
        out double cvRmsePercent,
        out double nmbePercent)
    {
        cvRmsePercent = 0;
        nmbePercent = 0;

        const int parameterCount = 3;
        if (fitObservations.Count <= parameterCount)
        {
            return false;
        }

        var meanActual = fitObservations.Average(observation => observation.Energy);
        if (!double.IsFinite(meanActual) || Math.Abs(meanActual) < 0.000001)
        {
            return false;
        }

        var squaredErrorSum = 0d;
        var biasSum = 0d;

        foreach (var observation in fitObservations)
        {
            var predicted = model.Predict(observation.AverageOutdoorTemperatureC);
            var residual = observation.Energy - predicted;
            squaredErrorSum += residual * residual;
            biasSum += residual;
        }

        var degreesOfFreedom = fitObservations.Count - parameterCount;
        if (degreesOfFreedom <= 0)
        {
            return false;
        }

        var rmse = Math.Sqrt(squaredErrorSum / degreesOfFreedom);
        if (!double.IsFinite(rmse))
        {
            return false;
        }

        cvRmsePercent = (rmse / meanActual) * 100.0;
        nmbePercent = (biasSum / (degreesOfFreedom * meanActual)) * 100.0;
        return double.IsFinite(cvRmsePercent) && double.IsFinite(nmbePercent);
    }

    private static bool TrySolveLinearSystem3x3(double[,] matrix, double[] rhs, out double[] solution)
    {
        solution = new double[3];
        var augmented = new double[3, 4];

        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 3; column++)
            {
                augmented[row, column] = matrix[row, column];
            }

            augmented[row, 3] = rhs[row];
        }

        for (var pivotIndex = 0; pivotIndex < 3; pivotIndex++)
        {
            var pivotRow = pivotIndex;
            var pivotMagnitude = Math.Abs(augmented[pivotIndex, pivotIndex]);
            for (var candidateRow = pivotIndex + 1; candidateRow < 3; candidateRow++)
            {
                var candidateMagnitude = Math.Abs(augmented[candidateRow, pivotIndex]);
                if (candidateMagnitude > pivotMagnitude)
                {
                    pivotMagnitude = candidateMagnitude;
                    pivotRow = candidateRow;
                }
            }

            if (pivotMagnitude < 1e-9)
            {
                return false;
            }

            if (pivotRow != pivotIndex)
            {
                for (var column = pivotIndex; column < 4; column++)
                {
                    (augmented[pivotIndex, column], augmented[pivotRow, column]) = (augmented[pivotRow, column], augmented[pivotIndex, column]);
                }
            }

            var pivot = augmented[pivotIndex, pivotIndex];
            for (var column = pivotIndex; column < 4; column++)
            {
                augmented[pivotIndex, column] /= pivot;
            }

            for (var row = 0; row < 3; row++)
            {
                if (row == pivotIndex)
                {
                    continue;
                }

                var factor = augmented[row, pivotIndex];
                if (Math.Abs(factor) < 1e-12)
                {
                    continue;
                }

                for (var column = pivotIndex; column < 4; column++)
                {
                    augmented[row, column] -= factor * augmented[pivotIndex, column];
                }
            }
        }

        solution[0] = augmented[0, 3];
        solution[1] = augmented[1, 3];
        solution[2] = augmented[2, 3];
        return solution.All(value => double.IsFinite(value));
    }

    private static DateTime MaxDateTime(DateTime left, DateTime right)
        => left >= right ? left : right;

    private static DateTime MinDateTime(DateTime left, DateTime right)
        => left <= right ? left : right;

    private async Task<(DateTime? MinUtc, DateTime? MaxUtc)> GetTimeDomainUtcForSourcesAsync(
        IEnumerable<CuratedNodeSource> sources,
        CancellationToken ct)
    {
        DateTime? minUtc = null;
        DateTime? maxUtc = null;
        var visitedFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources)
        {
            var filePath = ResolveCuratedFilePath(source);
            if (filePath is null || !visitedFilePaths.Add(filePath))
            {
                continue;
            }

            (DateTime? MinUtc, DateTime? MaxUtc) domain;
            if (_timeDomainCache.TryGetValue(filePath, out var cachedDomain))
            {
                domain = (cachedDomain.MinUtc, cachedDomain.MaxUtc);
            }
            else
            {
                domain = await GetTimeDomainUtcAsync(filePath, ct);
                if (domain.MinUtc.HasValue && domain.MaxUtc.HasValue)
                {
                    _timeDomainCache[filePath] = (domain.MinUtc.Value, domain.MaxUtc.Value);
                }
            }

            if (!domain.MinUtc.HasValue || !domain.MaxUtc.HasValue)
            {
                continue;
            }

            if (!minUtc.HasValue || domain.MinUtc.Value < minUtc.Value)
            {
                minUtc = domain.MinUtc.Value;
            }

            if (!maxUtc.HasValue || domain.MaxUtc.Value > maxUtc.Value)
            {
                maxUtc = domain.MaxUtc.Value;
            }
        }

        return (minUtc, maxUtc);
    }

    private static string ResolveSelectionSignalAggregateInterpretationNote(
        FacilitySignalFamily signalFamily,
        FacilitySignalSeriesSemantics seriesSemantics,
        CuratedNodeTimeSeriesGranularity granularity)
    {
        if (seriesSemantics == FacilitySignalSeriesSemantics.CumulativeCounter)
        {
            return granularity switch
            {
                CuratedNodeTimeSeriesGranularity.HourlyAverage => "The chart shows the sum of hourly interval-energy deltas derived from cumulative counters across matching scope nodes.",
                CuratedNodeTimeSeriesGranularity.DailyAverage => "The chart shows the sum of daily interval-energy deltas derived from cumulative counters across matching scope nodes.",
                _ => "The chart shows the sum of interval deltas derived from cumulative counters across matching scope nodes."
            };
        }

        return signalFamily switch
        {
            FacilitySignalFamily.Power => granularity switch
            {
                CuratedNodeTimeSeriesGranularity.HourlyAverage => "The chart shows the sum of hourly average power values across matching scope nodes.",
                CuratedNodeTimeSeriesGranularity.DailyAverage => "The chart shows the sum of daily average power values across matching scope nodes.",
                _ => "The chart shows the sum of instantaneous power values across matching scope nodes."
            },
            FacilitySignalFamily.Energy => granularity switch
            {
                CuratedNodeTimeSeriesGranularity.HourlyAverage => "The chart shows the sum of hourly average energy-series values across matching scope nodes.",
                CuratedNodeTimeSeriesGranularity.DailyAverage => "The chart shows the sum of daily average energy-series values across matching scope nodes.",
                _ => "The chart shows the sum of matching energy-series values across scope nodes."
            },
            _ => "The chart shows the selected signal in the current scope."
        };
    }

    /// <summary>
    /// OtevĹ™e StreamReader pro CSV soubor â€” automaticky detekuje .gz a obalĂ­ GZipStream.
    /// VolajĂ­cĂ­ je odpovÄ›dnĂ˝ za dispose (using).
    /// </summary>
    private static StreamReader OpenCsvReader(string filePath)
    {
        if (filePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            var fileStream = File.OpenRead(filePath);
            var gzStream = new System.IO.Compression.GZipStream(
                fileStream, System.IO.Compression.CompressionMode.Decompress);
            return new StreamReader(gzStream);
        }
        return new StreamReader(filePath);
    }

    /// <summary>
    /// PĹ™eloĹľĂ­ CuratedNodeSource na cestu k souboru.
    /// Pro binding-based uzly (MeterFolder != null): DataRootPath/meterFolder/fileName.
    /// Pro legacy uzly: DataSet/curated, DataSet/data, DataSet.
    /// </summary>
    private string? ResolveCuratedFilePath(CuratedNodeSource source)
    {
        if (!string.IsNullOrWhiteSpace(source.SourceFilePath) && File.Exists(source.SourceFilePath))
        {
            return source.SourceFilePath;
        }

        // Binding-based zdroj (novĂ˝ dataset)
        if (!string.IsNullOrEmpty(source.MeterFolder))
        {
            var bindingPath = _bindingRegistry.ResolveFilePath(source.MeterFolder, source.FileName);
            if (bindingPath is not null) return bindingPath;
        }

        // Legacy fallback
        return ResolveCuratedFilePath(source.FileName);
    }

    /// <summary>Legacy: hledĂˇ soubor v DataSet/curated, DataSet/data, DataSet.</summary>
    private string? ResolveCuratedFilePath(string fileName)
    {
        var curatedPath = Path.Combine(_env.ContentRootPath, "..", "DataSet", "curated", fileName);
        if (File.Exists(curatedPath)) return curatedPath;

        var dataPath = Path.Combine(_env.ContentRootPath, "..", "DataSet", "data", fileName);
        if (File.Exists(dataPath)) return dataPath;

        var rootPath = Path.Combine(_env.ContentRootPath, "..", "DataSet", fileName);
        if (File.Exists(rootPath)) return rootPath;

        return null;
    }

    private async Task<CuratedNodeDeviationSummary> CalculateDeviationAsync(string nodeKey, string filePath, CuratedNodeSource source, DateTime from, DateTime to, CancellationToken ct)
    {
        var intervalDuration = to - from;
        if (intervalDuration <= TimeSpan.Zero)
        {
            return new CuratedNodeDeviationSummary
            {
                IsAvailable = false,
                Unit = source.Unit,
                Methodology = BaselineMethodology,
                Message = "The selected time range is invalid for baseline calculation."
            };
        }

        var baselineCandidates = BuildBaselineCandidates(from, to);
        if (baselineCandidates.Count == 0)
        {
            return new CuratedNodeDeviationSummary
            {
                IsAvailable = false,
                Unit = source.Unit,
                Methodology = BaselineMethodology,
                Message = "Baseline cannot be prepared because no reference baseline windows are available."
            };
        }

        try
        {
            using var reader = OpenCsvReader(filePath);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(headerLine))
            {
                return new CuratedNodeDeviationSummary
                {
                    IsAvailable = false,
                    Unit = source.Unit,
                    Methodology = BaselineMethodology,
                    Message = "The reduced source is empty."
                };
            }

            var headers = headerLine.Split(',');
            int valueColIndex = Array.FindIndex(headers, h => h.Trim().Equals(source.ColumnName, StringComparison.OrdinalIgnoreCase));
            int timeColIndex = Array.FindIndex(headers, h =>
                h.Trim().Equals("datetime_utc", StringComparison.OrdinalIgnoreCase) ||
                h.Trim().Equals("timestamp", StringComparison.OrdinalIgnoreCase) ||
                h.Trim().Equals("time", StringComparison.OrdinalIgnoreCase));

            if (valueColIndex == -1)
            {
                return new CuratedNodeDeviationSummary
                {
                    IsAvailable = false,
                    Unit = source.Unit,
                    Methodology = BaselineMethodology,
                    Message = "The reduced source does not contain the expected column for the selected node."
                };
            }

            if (timeColIndex == -1)
            {
                timeColIndex = 0;
            }

            var currentStats = new RunningStats();
            var baselineStats = baselineCandidates.ToDictionary(candidate => candidate, _ => new RunningStats());
            var sampleStepHours = DefaultPowerSampleStepHours;
            DateTime? previousTimestamp = null;

            var minRelevantFrom = baselineCandidates.Min(candidate => candidate.From);
            if (from < minRelevantFrom)
            {
                minRelevantFrom = from;
            }

            var maxRelevantTo = baselineCandidates.Max(candidate => candidate.To);
            if (to > maxRelevantTo)
            {
                maxRelevantTo = to;
            }

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                var cols = line.Split(',');
                if (cols.Length <= Math.Max(timeColIndex, valueColIndex))
                {
                    continue;
                }

                if (!TryParseTimestamp(cols[timeColIndex], out var timestamp))
                {
                    continue;
                }

                UpdateSampleStepHours(previousTimestamp, timestamp, ref sampleStepHours);
                previousTimestamp = timestamp;

                if (timestamp < minRelevantFrom)
                {
                    continue;
                }

                if (timestamp >= maxRelevantTo)
                {
                    break;
                }

                if (!double.TryParse(cols[valueColIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                {
                    continue;
                }

                var normalizedValue = NormalizeValueForAggregation(value, source, sampleStepHours);

                if (timestamp >= from && timestamp < to)
                {
                    currentStats.Add(normalizedValue);
                }

                foreach (var candidate in baselineCandidates)
                {
                    if (timestamp >= candidate.From && timestamp < candidate.To)
                    {
                        baselineStats[candidate].Add(normalizedValue);
                    }
                }
            }

            if (currentStats.Count == 0)
            {
                return new CuratedNodeDeviationSummary
                {
                    IsAvailable = false,
                    Unit = source.Unit,
                    Methodology = BaselineMethodology,
                    Message = "No data is available for the selected interval."
                };
            }

            var minimumReferenceSamples = Math.Max(2, (int)Math.Round(currentStats.Count * MinimumReferenceCoverageRatio));

            var samePeriodReferenceAggregates = baselineCandidates
                .Where(candidate => candidate.Kind == BaselineReferenceKind.SamePeriodPreviousYear)
                .Select(candidate => new BaselineReferenceAggregate(candidate, baselineStats[candidate].Sum))
                .Where(aggregate => baselineStats[aggregate.Candidate].Count >= minimumReferenceSamples)
                .ToList();

            var recentComparableReferenceAggregates = baselineCandidates
                .Where(candidate => candidate.Kind == BaselineReferenceKind.RecentComparablePeriod)
                .Select(candidate => new BaselineReferenceAggregate(candidate, baselineStats[candidate].Sum))
                .Where(aggregate => baselineStats[aggregate.Candidate].Count >= minimumReferenceSamples)
                .ToList();

            var selectedBaseline = SelectBaselineValue(samePeriodReferenceAggregates, recentComparableReferenceAggregates);
            if (selectedBaseline is null)
            {
                return new CuratedNodeDeviationSummary
                {
                    IsAvailable = false,
                    Unit = source.Unit,
                    Methodology = BaselineMethodology,
                    Message = "Baseline cannot be calculated because no reference baseline windows have sufficient data coverage."
                };
            }

            var baselineValue = selectedBaseline.Value;
            var minimumMeaningfulBaseline = GetMinimumMeaningfulBaseline(intervalDuration, source);
            if (Math.Abs(baselineValue) < minimumMeaningfulBaseline)
            {
                return new CuratedNodeDeviationSummary
                {
                    IsAvailable = false,
                    Unit = source.Unit,
                    Methodology = BuildMethodologyText(selectedBaseline.StrategyDescription, from, to, minimumReferenceSamples),
                    Message = "Baseline is too low in this interval for stable percentage-based deviation evaluation."
                };
            }

            var currentValue = currentStats.Sum;
            var deltaAbsolute = currentValue - baselineValue;
            var deltaPercent = (deltaAbsolute / baselineValue) * 100.0;
            var weatherExplanation = await BuildWeatherExplanationAsync(nodeKey, from, to, selectedBaseline.ReferenceCandidates, deltaAbsolute, minimumReferenceSamples, ct);

            return new CuratedNodeDeviationSummary
            {
                IsAvailable = true,
                CurrentValue = currentValue,
                BaselineValue = baselineValue,
                DeltaAbsolute = deltaAbsolute,
                DeltaPercent = deltaPercent,
                Severity = ClassifySeverity(deltaPercent),
                ReferenceIntervalsUsed = selectedBaseline.ReferenceCandidates.Count,
                Unit = source.Unit,
                Methodology = BuildMethodologyText(selectedBaseline.StrategyDescription, from, to, minimumReferenceSamples),
                WeatherExplanation = weatherExplanation
            };
        }
        catch
        {
            return new CuratedNodeDeviationSummary
            {
                IsAvailable = false,
                Unit = source.Unit,
                Methodology = BaselineMethodology,
                Message = "The reduced source for baseline calculation could not be loaded."
            };
        }
    }

    private static IReadOnlyList<BaselineCandidate> BuildBaselineCandidates(DateTime from, DateTime to)
    {
        var duration = to - from;
        if (duration <= TimeSpan.Zero)
        {
            return [];
        }

        var candidates = new List<BaselineCandidate>();

        for (var yearOffset = 1; yearOffset <= MaxHistoricalYearsForBaseline; yearOffset++)
        {
            if (!TryShiftYearsBack(from, to, yearOffset, out var baselineFrom, out var baselineTo))
            {
                continue;
            }

            candidates.Add(new BaselineCandidate(
                baselineFrom,
                baselineTo,
                BaselineReferenceKind.SamePeriodPreviousYear,
                $"Same period last year (-{yearOffset}y)"));
        }

        for (var offsetIndex = 1; offsetIndex <= RecentComparableWindowsForBaseline; offsetIndex++)
        {
            var offsetTicks = duration.Ticks * offsetIndex;
            if (offsetTicks <= 0)
            {
                continue;
            }

            if (from.Ticks <= offsetTicks || to.Ticks <= offsetTicks)
            {
                continue;
            }

            var offset = TimeSpan.FromTicks(offsetTicks);
            var baselineFrom = from - offset;
            var baselineTo = to - offset;

            if (baselineTo <= baselineFrom)
            {
                continue;
            }

            candidates.Add(new BaselineCandidate(
                baselineFrom,
                baselineTo,
                BaselineReferenceKind.RecentComparablePeriod,
                $"Previous comparable period (-{offsetIndex}x interval)"));
        }

        return candidates;
    }

    private static bool TryShiftYearsBack(DateTime from, DateTime to, int yearsBack, out DateTime shiftedFrom, out DateTime shiftedTo)
    {
        shiftedFrom = default;
        shiftedTo = default;

        if (yearsBack <= 0)
        {
            return false;
        }

        if (from.Year - yearsBack < DateTime.MinValue.Year || to.Year - yearsBack < DateTime.MinValue.Year)
        {
            return false;
        }

        try
        {
            shiftedFrom = from.AddYears(-yearsBack);
            shiftedTo = to.AddYears(-yearsBack);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        return shiftedTo > shiftedFrom;
    }

    private static BaselineSelection? SelectBaselineValue(
        IReadOnlyList<BaselineReferenceAggregate> samePeriodReferenceAggregates,
        IReadOnlyList<BaselineReferenceAggregate> recentComparableReferenceAggregates)
    {
        if (samePeriodReferenceAggregates.Count >= 2)
        {
            return new BaselineSelection(
                Median(samePeriodReferenceAggregates.Select(x => x.Sum).ToList()),
                "Same period in previous years (median)",
                samePeriodReferenceAggregates.Select(x => x.Candidate).ToList()
            );
        }

        if (samePeriodReferenceAggregates.Count == 1 && recentComparableReferenceAggregates.Count >= 2)
        {
            var samePeriod = samePeriodReferenceAggregates[0].Sum;
            var recentMedian = Median(recentComparableReferenceAggregates.Select(x => x.Sum).ToList());
            var referenceCandidates = samePeriodReferenceAggregates.Select(x => x.Candidate)
                .Concat(recentComparableReferenceAggregates.Select(x => x.Candidate))
                .ToList();
            return new BaselineSelection(
                (samePeriod * 0.70) + (recentMedian * 0.30),
                "Hybrid: same period last year + recent comparable periods",
                referenceCandidates
            );
        }

        if (samePeriodReferenceAggregates.Count == 1)
        {
            return new BaselineSelection(
                samePeriodReferenceAggregates[0].Sum,
                "Same period last year",
                [samePeriodReferenceAggregates[0].Candidate]
            );
        }

        if (recentComparableReferenceAggregates.Count >= 2)
        {
            return new BaselineSelection(
                Median(recentComparableReferenceAggregates.Select(x => x.Sum).ToList()),
                "Previous comparable periods of the same length (median)",
                recentComparableReferenceAggregates.Select(x => x.Candidate).ToList()
            );
        }

        if (recentComparableReferenceAggregates.Count == 1)
        {
            return new BaselineSelection(
                recentComparableReferenceAggregates[0].Sum,
                "Immediately previous comparable period",
                [recentComparableReferenceAggregates[0].Candidate]
            );
        }

        return null;
    }

    private static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var ordered = values.OrderBy(v => v).ToArray();
        var mid = ordered.Length / 2;

        if (ordered.Length % 2 == 0)
        {
            return (ordered[mid - 1] + ordered[mid]) / 2.0;
        }

        return ordered[mid];
    }

    private static double Percentile(IReadOnlyList<double> values, double percentile)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var clampedPercentile = Math.Clamp(percentile, 0.0, 1.0);
        var ordered = values.OrderBy(v => v).ToArray();
        if (ordered.Length == 1)
        {
            return ordered[0];
        }

        var position = (ordered.Length - 1) * clampedPercentile;
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);
        if (lowerIndex == upperIndex)
        {
            return ordered[lowerIndex];
        }

        var weight = position - lowerIndex;
        return ordered[lowerIndex] + (ordered[upperIndex] - ordered[lowerIndex]) * weight;
    }

    private static double GetMinimumSafePowerRatioDenominator(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0.000001d;
        }

        var maxMagnitude = values.Max(value => Math.Abs(value));
        return Math.Max(0.000001d, maxMagnitude * 0.000001d);
    }

    private static double StandardDeviation(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var mean = values.Average();
        var variance = values.Sum(value => Math.Pow(value - mean, 2)) / values.Count;
        return Math.Sqrt(Math.Max(0, variance));
    }

    private static double GetMinimumMeaningfulBaseline(TimeSpan intervalDuration, CuratedNodeSource source)
    {
        if (!source.IsPowerSignal)
        {
            return 0.000001;
        }

        // U velmi nĂ­zkĂ© baseline je procentnĂ­ odchylka numericky nestabilnĂ­.
        return Math.Max(0.5, intervalDuration.TotalHours * 0.05);
    }

    private static string BuildMethodologyText(string strategyDescription, DateTime from, DateTime to, int minimumReferenceSamples)
    {
        return $"{BaselineMethodology} Strategy: {strategyDescription}. Analysis window: {from:u} - {to:u}. Minimum reference-window coverage: {minimumReferenceSamples} samples.";
    }

    private async Task<WeatherExplanationSummary?> BuildWeatherExplanationAsync(
        string nodeKey,
        DateTime analysisFrom,
        DateTime analysisTo,
        IReadOnlyList<BaselineCandidate> referenceCandidates,
        double deltaAbsolute,
        int minimumReferenceSamples,
        CancellationToken ct)
    {
        if (nodeKey is not ("heating_main" or "cooling_main"))
        {
            return null;
        }

        var weatherFilePath = ResolveCuratedFilePath("weather.csv");
        if (weatherFilePath is null)
        {
            return CreateUnavailableWeatherExplanation("Weather explanation is unavailable because the weather.csv source is missing.");
        }

        var weatherAverages = await GetWeatherAveragesAsync(weatherFilePath, analysisFrom, analysisTo, referenceCandidates, minimumReferenceSamples, ct);
        if (!weatherAverages.CurrentAverageTempC.HasValue)
        {
            return CreateUnavailableWeatherExplanation("Weather explanation is unavailable because outdoor-temperature data is missing in the current interval.");
        }

        if (weatherAverages.ReferenceAverageTempC.Count == 0)
        {
            return CreateUnavailableWeatherExplanation("Weather explanation is unavailable because the reference baseline period lacks sufficient weather data.");
        }

        var referenceAverage = weatherAverages.ReferenceAverageTempC.Count >= 2
            ? Median(weatherAverages.ReferenceAverageTempC)
            : weatherAverages.ReferenceAverageTempC[0];
        var currentAverage = weatherAverages.CurrentAverageTempC.Value;
        var deltaTemp = currentAverage - referenceAverage;

        var status = WeatherExplanationStatus.NotSupportedByWeather;
        var conclusion = "Weather does not support the deviation in this interval.";

        if (Math.Abs(deltaTemp) < WeatherExplanationDeltaThresholdC)
        {
            return new WeatherExplanationSummary
            {
                IsAvailable = true,
                Status = WeatherExplanationStatus.WeatherChangeNeutral,
                CurrentAverageOutdoorTempC = currentAverage,
                ReferenceAverageOutdoorTempC = referenceAverage,
                DeltaOutdoorTempC = deltaTemp,
                Conclusion = "The weather difference versus the reference is small.",
                Methodology = "Explanatory heuristic v1 compares the average outdoor temperature in the analysis window with the reference baseline windows. When |delta T| < 0.8 °C, the weather change is treated as small."
            };
        }

        if (nodeKey == "heating_main")
        {
            var isColderThanReference = deltaTemp <= -WeatherExplanationDeltaThresholdC;
            var isWarmerThanReference = deltaTemp >= WeatherExplanationDeltaThresholdC;

            if ((deltaAbsolute > 0 && isColderThanReference) || (deltaAbsolute < 0 && isWarmerThanReference))
            {
                status = WeatherExplanationStatus.SupportedByWeather;
                conclusion = "The deviation may be partially explained by weather.";
            }
        }
        else if (nodeKey == "cooling_main")
        {
            var isWarmerThanReference = deltaTemp >= WeatherExplanationDeltaThresholdC;
            var isColderThanReference = deltaTemp <= -WeatherExplanationDeltaThresholdC;

            if ((deltaAbsolute > 0 && isWarmerThanReference) || (deltaAbsolute < 0 && isColderThanReference))
            {
                status = WeatherExplanationStatus.SupportedByWeather;
                conclusion = "The deviation may be partially explained by weather.";
            }
        }

        return new WeatherExplanationSummary
        {
            IsAvailable = true,
            Status = status,
            CurrentAverageOutdoorTempC = currentAverage,
            ReferenceAverageOutdoorTempC = referenceAverage,
            DeltaOutdoorTempC = deltaTemp,
            Conclusion = conclusion,
            Methodology = "Explanatory heuristic v1 compares the average outdoor temperature in the analysis window with the baseline windows used by the selected baseline strategy."
        };
    }

    private static WeatherExplanationSummary CreateUnavailableWeatherExplanation(string message)
    {
        return new WeatherExplanationSummary
        {
            IsAvailable = false,
            Status = WeatherExplanationStatus.Unavailable,
            Conclusion = message,
            Methodology = "The explanatory heuristic requires available weather data for both the current and reference periods."
        };
    }

    private async Task<(double? CurrentAverageTempC, List<double> ReferenceAverageTempC)> GetWeatherAveragesAsync(
        string weatherFilePath,
        DateTime analysisFrom,
        DateTime analysisTo,
        IReadOnlyList<BaselineCandidate> referenceCandidates,
        int minimumReferenceSamples,
        CancellationToken ct)
    {
        try
        {
            using var reader = OpenCsvReader(weatherFilePath);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(headerLine))
            {
                return (null, []);
            }

            var headers = headerLine.Split(',');
            int valueColIndex = Array.FindIndex(headers, h => h.Trim().Equals("WeatherStation.Weather.Ta", StringComparison.OrdinalIgnoreCase));
            int timeColIndex = Array.FindIndex(headers, h =>
                h.Trim().Equals("datetime_utc", StringComparison.OrdinalIgnoreCase) ||
                h.Trim().Equals("timestamp", StringComparison.OrdinalIgnoreCase) ||
                h.Trim().Equals("time", StringComparison.OrdinalIgnoreCase));

            if (valueColIndex == -1)
            {
                return (null, []);
            }

            if (timeColIndex == -1)
            {
                timeColIndex = 0;
            }

            var currentStats = new RunningStats();
            var referenceStats = referenceCandidates.ToDictionary(candidate => candidate, _ => new RunningStats());

            var minRelevantFrom = referenceCandidates.Count > 0
                ? referenceCandidates.Min(candidate => candidate.From)
                : analysisFrom;
            if (analysisFrom < minRelevantFrom)
            {
                minRelevantFrom = analysisFrom;
            }

            var maxRelevantTo = referenceCandidates.Count > 0
                ? referenceCandidates.Max(candidate => candidate.To)
                : analysisTo;
            if (analysisTo > maxRelevantTo)
            {
                maxRelevantTo = analysisTo;
            }

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                var cols = line.Split(',');
                if (cols.Length <= Math.Max(timeColIndex, valueColIndex))
                {
                    continue;
                }

                if (!TryParseTimestamp(cols[timeColIndex], out var timestamp))
                {
                    continue;
                }

                if (timestamp < minRelevantFrom)
                {
                    continue;
                }

                if (timestamp >= maxRelevantTo)
                {
                    break;
                }

                if (!double.TryParse(cols[valueColIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                {
                    continue;
                }

                if (timestamp >= analysisFrom && timestamp < analysisTo)
                {
                    currentStats.Add(value);
                }

                foreach (var candidate in referenceCandidates)
                {
                    if (timestamp >= candidate.From && timestamp < candidate.To)
                    {
                        referenceStats[candidate].Add(value);
                    }
                }
            }

            var currentAverage = currentStats.Count > 0
                ? currentStats.Sum / currentStats.Count
                : (double?)null;

            var referenceAverages = referenceCandidates
                .Where(candidate => referenceStats[candidate].Count >= minimumReferenceSamples)
                .Select(candidate => referenceStats[candidate].Sum / referenceStats[candidate].Count)
                .ToList();

            return (currentAverage, referenceAverages);
        }
        catch
        {
            return (null, []);
        }
    }

    private async Task<(DateTime? MinUtc, DateTime? MaxUtc)> GetTimeDomainUtcAsync(string filePath, CancellationToken ct)
    {
        try
        {
            using var reader = OpenCsvReader(filePath);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(headerLine))
            {
                return (null, null);
            }

            var headers = headerLine.Split(',');
            int timeColIndex = Array.FindIndex(headers, h =>
                h.Trim().Equals("datetime_utc", StringComparison.OrdinalIgnoreCase) ||
                h.Trim().Equals("timestamp", StringComparison.OrdinalIgnoreCase) ||
                h.Trim().Equals("time", StringComparison.OrdinalIgnoreCase));

            if (timeColIndex == -1)
            {
                timeColIndex = 0;
            }

            DateTime? minUtc = null;
            DateTime? maxUtc = null;

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                var cols = line.Split(',');
                if (cols.Length <= timeColIndex)
                {
                    continue;
                }

                if (!TryParseTimestamp(cols[timeColIndex], out var ts))
                {
                    continue;
                }

                if (!minUtc.HasValue || ts < minUtc.Value)
                {
                    minUtc = ts;
                }

                if (!maxUtc.HasValue || ts > maxUtc.Value)
                {
                    maxUtc = ts;
                }
            }

            return (minUtc, maxUtc);
        }
        catch
        {
            return (null, null);
        }
    }

    private static bool TryParseTimestamp(string raw, out DateTime timestamp)
    {
        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
        {
            timestamp = dto.UtcDateTime;
            return true;
        }

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
        {
            timestamp = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return true;
        }

        timestamp = default;
        return false;
    }

    private async Task<DateTime?> GetMaxTimestampUtcAsync(string filePath, CancellationToken ct)
    {
        try
        {
            using var reader = OpenCsvReader(filePath);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(headerLine))
            {
                return null;
            }

            var headers = headerLine.Split(',');
            int timeColIndex = Array.FindIndex(headers, h =>
                h.Trim().Equals("datetime_utc", StringComparison.OrdinalIgnoreCase) ||
                h.Trim().Equals("timestamp", StringComparison.OrdinalIgnoreCase) ||
                h.Trim().Equals("time", StringComparison.OrdinalIgnoreCase));

            if (timeColIndex == -1)
            {
                timeColIndex = 0;
            }

            DateTime? maxTimestamp = null;
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                var cols = line.Split(',');
                if (cols.Length <= timeColIndex)
                {
                    continue;
                }

                if (!TryParseTimestamp(cols[timeColIndex], out var timestamp))
                {
                    continue;
                }

                if (!maxTimestamp.HasValue || timestamp > maxTimestamp.Value)
                {
                    maxTimestamp = timestamp;
                }
            }

            return maxTimestamp;
        }
        catch
        {
            return null;
        }
    }

    private static bool IntervalsOverlap(DateTime aFrom, DateTime aTo, DateTime bFrom, DateTime bTo)
    {
        return aFrom < bTo && bFrom < aTo;
    }

    private static NodeDeviationSeverity ClassifySeverity(double deltaPercent)
    {
        var abs = Math.Abs(deltaPercent);
        if (abs > 25.0)
        {
            return NodeDeviationSeverity.High;
        }

        if (abs >= 10.0)
        {
            return NodeDeviationSeverity.Elevated;
        }

        return NodeDeviationSeverity.Normal;
    }

    private static void UpdateSampleStepHours(DateTime? previousTimestamp, DateTime currentTimestamp, ref double sampleStepHours)
    {
        if (!previousTimestamp.HasValue)
        {
            return;
        }

        var deltaHours = (currentTimestamp - previousTimestamp.Value).TotalHours;
        if (deltaHours > 0 && deltaHours <= 6)
        {
            sampleStepHours = deltaHours;
        }
    }

    private static double NormalizeValueForStats(double rawValue, CuratedNodeSource source)
    {
        if (!source.IsPowerSignal)
        {
            return rawValue;
        }

        return rawValue * source.PowerToKilowattFactor;
    }

    private static string? ResolveImplicitPowerUnit(FacilityDataBindingRegistry.BindingRecord binding)
    {
        if (!string.IsNullOrWhiteSpace(binding.Unit))
        {
            return binding.Unit;
        }

        return binding.UsesFixedCsvSeriesFormat ? null : "W";
    }

    private static double ResolvePowerToKilowattFactor(string? rawUnit)
    {
        return rawUnit?.Trim().ToLowerInvariant() switch
        {
            "w" => 0.001,
            "kw" => 1.0,
            "mw" => 1000.0,
            _ => 1.0,
        };
    }

    private static double ResolveEnergyToKilowattHourFactor(string? rawUnit)
    {
        return rawUnit?.Trim().ToLowerInvariant() switch
        {
            "wh" => 0.001,
            "kwh" => 1.0,
            "mwh" => 1000.0,
            "gwh" => 1000000.0,
            _ => 1.0,
        };
    }

    private static double NormalizeValueForAggregation(double rawValue, CuratedNodeSource source, double sampleStepHours)
    {
        if (!source.IsPowerSignal)
        {
            return rawValue;
        }

        var powerKw = NormalizeValueForStats(rawValue, source);
        return powerKw * sampleStepHours;
    }

    private static bool TryResolveSeriesSampleValues(
        double rawValue,
        CuratedNodeSource source,
        double sampleStepHours,
        ref double? previousCounterValue,
        out double statsValue,
        out double aggregationValue)
    {
        if (source.SeriesSemantics != FacilitySignalSeriesSemantics.CumulativeCounter)
        {
            statsValue = NormalizeValueForStats(rawValue, source);
            aggregationValue = NormalizeValueForAggregation(rawValue, source, sampleStepHours);
            return true;
        }

        if (!previousCounterValue.HasValue)
        {
            previousCounterValue = rawValue;
            statsValue = 0;
            aggregationValue = 0;
            return false;
        }

        var delta = rawValue - previousCounterValue.Value;
        previousCounterValue = rawValue;

        if (!double.IsFinite(delta) || delta < 0)
        {
            statsValue = 0;
            aggregationValue = 0;
            return false;
        }

        statsValue = delta;
        aggregationValue = delta;
        return true;
    }

    private static double ResolveBucketValue(RunningStats stats, CuratedNodeSource source)
    {
        if (source.SeriesSemantics == FacilitySignalSeriesSemantics.CumulativeCounter)
        {
            return stats.Sum;
        }

        return stats.Sum / stats.Count;
    }

    private static string? ResolveSeriesStatusMessage(CuratedNodeSource source, CuratedNodeTimeSeriesGranularity granularity)
    {
        if (source.SeriesSemantics != FacilitySignalSeriesSemantics.CumulativeCounter)
        {
            return null;
        }

        return granularity switch
        {
            CuratedNodeTimeSeriesGranularity.HourlyAverage => "Derived interval view: the cumulative counter is converted to interval deltas and summed per hour.",
            CuratedNodeTimeSeriesGranularity.DailyAverage => "Derived interval view: the cumulative counter is converted to interval deltas and summed per day.",
            _ => "Derived interval view: the cumulative counter is converted to interval deltas between consecutive samples."
        };
    }

    private static string ResolveTimeSeriesUnit(CuratedNodeSource source)
    {
        return !string.IsNullOrWhiteSpace(source.StatsUnit)
            ? source.StatsUnit
            : source.Unit;
    }

    private static string ResolveTimeSeriesYAxisLabel(CuratedNodeSource source)
    {
        var unit = ResolveTimeSeriesUnit(source);
        return $"{source.StatsLabel} ({unit})";
    }

    private static TimeSeriesGranularityDecision ResolveTimeSeriesGranularity(DateTime from, DateTime to, CuratedNodeSource source, CuratedNodeTimeSeriesMode requestedMode)
    {
        var duration = to - from;
        var valueKind = source.IsPowerSignal ? "power" : "value";

        if (source.SeriesSemantics == FacilitySignalSeriesSemantics.CumulativeCounter)
        {
            if (requestedMode == CuratedNodeTimeSeriesMode.Raw15Min)
            {
                return new TimeSeriesGranularityDecision(
                    CuratedNodeTimeSeriesGranularity.Raw15Min,
                    "Derived interval delta",
                    "Manual 15 min mode: the cumulative counter is converted to interval deltas between consecutive samples.",
                    CuratedNodeTimeSeriesMode.Raw15Min,
                    "15min"
                );
            }

            if (requestedMode == CuratedNodeTimeSeriesMode.HourlyAverage)
            {
                return new TimeSeriesGranularityDecision(
                    CuratedNodeTimeSeriesGranularity.HourlyAverage,
                    "Hourly energy sum",
                    "Manual hourly mode: interval deltas derived from the cumulative counter are summed within each hourly bucket.",
                    CuratedNodeTimeSeriesMode.HourlyAverage,
                    "Hourly"
                );
            }

            if (requestedMode == CuratedNodeTimeSeriesMode.DailyAverage)
            {
                return new TimeSeriesGranularityDecision(
                    CuratedNodeTimeSeriesGranularity.DailyAverage,
                    "Daily energy sum",
                    "Manual daily mode: interval deltas derived from the cumulative counter are summed within each daily bucket.",
                    CuratedNodeTimeSeriesMode.DailyAverage,
                    "Daily"
                );
            }

            if (duration <= RawTimeSeriesThreshold)
            {
                return new TimeSeriesGranularityDecision(
                    CuratedNodeTimeSeriesGranularity.Raw15Min,
                    "Derived interval delta",
                    "Auto mode: the cumulative counter is converted to interval deltas between consecutive samples.",
                    CuratedNodeTimeSeriesMode.Auto,
                    "Auto"
                );
            }

            if (duration <= HourlyTimeSeriesThreshold)
            {
                return new TimeSeriesGranularityDecision(
                    CuratedNodeTimeSeriesGranularity.HourlyAverage,
                    "Hourly energy sum",
                    "Auto mode: interval deltas derived from the cumulative counter are summed within each hourly bucket.",
                    CuratedNodeTimeSeriesMode.Auto,
                    "Auto"
                );
            }

            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.DailyAverage,
                "Daily energy sum",
                "Auto mode: interval deltas derived from the cumulative counter are summed within each daily bucket.",
                CuratedNodeTimeSeriesMode.Auto,
                "Auto"
            );
        }

        if (requestedMode == CuratedNodeTimeSeriesMode.Raw15Min)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.Raw15Min,
                source.IsPowerSignal ? "Raw 15 min power" : "Raw detailed series",
                "Manual 15 min mode: no aggregation is applied and the original time-series samples are shown.",
                CuratedNodeTimeSeriesMode.Raw15Min,
                "15min"
            );
        }

        if (requestedMode == CuratedNodeTimeSeriesMode.HourlyAverage)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.HourlyAverage,
                source.IsPowerSignal ? "Hourly average power" : "Hourly average value",
                $"Manual hourly mode: each point is the arithmetic average {valueKind} in its hourly bucket.",
                CuratedNodeTimeSeriesMode.HourlyAverage,
                "Hourly"
            );
        }

        if (requestedMode == CuratedNodeTimeSeriesMode.DailyAverage)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.DailyAverage,
                source.IsPowerSignal ? "Daily average power" : "Daily average value",
                $"Manual daily mode: each point is the arithmetic average {valueKind} in its daily bucket.",
                CuratedNodeTimeSeriesMode.DailyAverage,
                "Daily"
            );
        }

        if (duration <= RawTimeSeriesThreshold)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.Raw15Min,
                source.IsPowerSignal ? "Raw 15 min power" : "Raw detailed series",
                "Auto mode: no aggregation is applied and the original time-series samples are shown.",
                CuratedNodeTimeSeriesMode.Auto,
                "Auto"
            );
        }

        if (duration <= HourlyTimeSeriesThreshold)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.HourlyAverage,
                source.IsPowerSignal ? "Hourly average power" : "Hourly average value",
                $"Auto mode: hourly aggregation is applied and each point is the arithmetic average {valueKind} in its hourly bucket.",
                CuratedNodeTimeSeriesMode.Auto,
                "Auto"
            );
        }

        return new TimeSeriesGranularityDecision(
            CuratedNodeTimeSeriesGranularity.DailyAverage,
            source.IsPowerSignal ? "Daily average power" : "Daily average value",
            $"Auto mode: daily aggregation is applied and each point is the arithmetic average {valueKind} in its daily bucket.",
            CuratedNodeTimeSeriesMode.Auto,
            "Auto"
        );
    }

    private static CuratedNodeTimeSeriesMode ResolvePerformanceEvaluationMode(DateTime from, DateTime to)
    {
        return (to - from) <= RawTimeSeriesThreshold
            ? CuratedNodeTimeSeriesMode.Raw15Min
            : CuratedNodeTimeSeriesMode.HourlyAverage;
    }

    private static bool MatchesPerformanceEvaluationMode(CuratedNodeTimeSeriesResult aggregateTimeSeries, CuratedNodeTimeSeriesMode performanceMode)
    {
        return performanceMode switch
        {
            CuratedNodeTimeSeriesMode.Raw15Min => aggregateTimeSeries.Granularity == CuratedNodeTimeSeriesGranularity.Raw15Min,
            CuratedNodeTimeSeriesMode.HourlyAverage => aggregateTimeSeries.Granularity == CuratedNodeTimeSeriesGranularity.HourlyAverage,
            _ => false
        };
    }

    private static DateTime GetBucketStartUtc(DateTime timestampUtc, CuratedNodeTimeSeriesGranularity granularity)
    {
        return granularity switch
        {
            CuratedNodeTimeSeriesGranularity.HourlyAverage => new DateTime(timestampUtc.Year, timestampUtc.Month, timestampUtc.Day, timestampUtc.Hour, 0, 0, DateTimeKind.Utc),
            CuratedNodeTimeSeriesGranularity.DailyAverage => new DateTime(timestampUtc.Year, timestampUtc.Month, timestampUtc.Day, 0, 0, 0, DateTimeKind.Utc),
            _ => timestampUtc
        };
    }

    private static string ResolveTimeSeriesInterpretationNote(CuratedNodeSource source, TimeSeriesGranularityDecision granularity)
    {
        if (source.SeriesSemantics == FacilitySignalSeriesSemantics.CumulativeCounter)
        {
            return granularity.Granularity switch
            {
                CuratedNodeTimeSeriesGranularity.HourlyAverage => "The chart shows hourly interval-energy sums derived from a cumulative counter.",
                CuratedNodeTimeSeriesGranularity.DailyAverage => "The chart shows daily interval-energy sums derived from a cumulative counter.",
                _ => "The chart shows interval deltas derived from a cumulative counter. Each point is the increment between consecutive samples."
            };
        }

        if (source.IsPowerSignal)
        {
            return granularity.Granularity switch
            {
                CuratedNodeTimeSeriesGranularity.HourlyAverage => "The chart shows the hourly average power (kW). The summary above the chart remains interval energy (kWh).",
                CuratedNodeTimeSeriesGranularity.DailyAverage => "The chart shows the daily average power (kW). The summary above the chart remains interval energy (kWh).",
                _ => "The chart shows instantaneous power over time (kW) in the original time-series step (~15 min). The summary above the chart shows interval energy (kWh)."
            };
        }

        return granularity.Granularity switch
        {
            CuratedNodeTimeSeriesGranularity.HourlyAverage => "The chart shows the hourly average value for the selected node.",
            CuratedNodeTimeSeriesGranularity.DailyAverage => "The chart shows the daily average value for the selected node.",
            _ => "The chart shows the instantaneous value over time for the selected node."
        };
    }

    private string ResolveCompareSeriesLabel(string nodeKey)
    {
        var source = ResolveCuratedNodeSource(nodeKey);
        return !string.IsNullOrWhiteSpace(source?.Title)
            ? source.Title
            : nodeKey;
    }

    private static string ResolveCompareInterpretationNote(CuratedNodeTimeSeriesGranularity granularity)
    {
        return granularity switch
        {
            CuratedNodeTimeSeriesGranularity.HourlyAverage => "Compare preview shows power series over time (kW) for multiple nodes. Hourly aggregation means average power within each hourly bucket.",
            CuratedNodeTimeSeriesGranularity.DailyAverage => "Compare preview shows power series over time (kW) for multiple nodes. Daily aggregation means average power within each daily bucket.",
            _ => "Compare preview shows power series over time (kW) for multiple nodes in the original detailed data step (~15 min)."
        };
    }

    private static string ResolveSelectionAggregateInterpretationNote(CuratedNodeTimeSeriesGranularity granularity, CuratedAggregateEnergyProfile energyProfile)
    {
        var signedSemanticsNote = energyProfile switch
        {
            CuratedAggregateEnergyProfile.MixedSigned => " The selection combines load and generation: positive points represent net load, negative points represent net generation or export.",
            CuratedAggregateEnergyProfile.GenerationOnly => " The selection is generation or export oriented: negative points are expected and represent export or generation.",
            _ => string.Empty
        };

        return granularity switch
        {
            CuratedNodeTimeSeriesGranularity.HourlyAverage => "The aggregate chart shows the sum of hourly average power values (kW) across supported selection-set nodes." + signedSemanticsNote,
            CuratedNodeTimeSeriesGranularity.DailyAverage => "The aggregate chart shows the sum of daily average power values (kW) across supported selection-set nodes." + signedSemanticsNote,
            _ => "The aggregate chart shows the sum of instantaneous power values (kW) across supported selection-set nodes in the original data step (~15 min)." + signedSemanticsNote
        };
    }

    private static CuratedSelectionDisaggregationSummary BuildDisaggregationSummary(
        IReadOnlyList<CuratedSelectionContributionItem> breakdown,
        CuratedAggregateEnergyProfile energyProfile)
    {
        var measuredContributors = breakdown
            .Where(item => item.HasData)
            .ToList();

        var consumptionContributorCount = measuredContributors.Count(item => item.IntervalEnergyKwh > 0);
        var generationContributorCount = measuredContributors.Count(item => item.IntervalEnergyKwh < 0);
        var hasMixedSigns = consumptionContributorCount > 0 && generationContributorCount > 0;
        var totalAbsoluteContributionKwh = measuredContributors.Sum(item => Math.Abs(item.IntervalEnergyKwh));

        var compositionSummary = measuredContributors.Count switch
        {
            0 => "Disaggregation foundation: no measured contributors are available in the interval.",
            1 => $"The aggregate is composed of 1 measured contributor: {measuredContributors[0].Label}.",
            _ when hasMixedSigns => $"The aggregate is composed of {measuredContributors.Count} measured contributors (load {consumptionContributorCount}, generation {generationContributorCount}).",
            _ when energyProfile == CuratedAggregateEnergyProfile.GenerationOnly => $"The aggregate is composed of {measuredContributors.Count} measured contributors that reduce the net balance (generation/export).",
            _ => $"The aggregate is composed of {measuredContributors.Count} measured contributors that increase load (consumption)."
        };

        return new CuratedSelectionDisaggregationSummary
        {
            Methodology = "Measured-only disaggregation v1: aggregate je transparentne rozlozen pouze na zname podporovane contributory se skutecnymi daty v intervalu. Bez NILM a bez inferencniho modelu.",
            CompositionSummary = compositionSummary,
            MeasuredContributorCount = measuredContributors.Count,
            ConsumptionContributorCount = consumptionContributorCount,
            GenerationContributorCount = generationContributorCount,
            HasMixedSigns = hasMixedSigns,
            TotalAbsoluteContributionKwh = totalAbsoluteContributionKwh
        };
    }

    private static CuratedSelectionContributionIntelligenceSummary BuildContributionIntelligenceSummary(
        IReadOnlyList<CuratedSelectionContributionItem> breakdown,
        CuratedAggregateEnergyProfile energyProfile)
    {
        var measuredContributors = breakdown
            .Where(item => item.HasData)
            .ToList();

        var topContributors = measuredContributors
            .OrderByDescending(item => Math.Abs(item.IntervalEnergyKwh))
            .ThenBy(item => item.Label, StringComparer.CurrentCultureIgnoreCase)
            .Take(5)
            .Select(item => new CuratedSelectionTopContributorItem
            {
                NodeKey = item.NodeKey,
                Label = item.Label,
                NodeRole = item.NodeRole,
                NodeRoleLabel = item.NodeRoleLabel,
                IntervalEnergyKwh = item.IntervalEnergyKwh,
                AbsoluteSharePercent = item.SharePercent ?? 0,
                ContributionRole = item.ContributionRole,
                Direction = ResolveContributionDirection(item.IntervalEnergyKwh)
            })
            .ToList();

        var dominantSourceSummary = BuildDominantSourceSummary(measuredContributors, topContributors, energyProfile);

        return new CuratedSelectionContributionIntelligenceSummary
        {
            TopContributors = topContributors,
            DominantSourceSummary = dominantSourceSummary,
            Methodology = "Top contributors = max 5 contributoru podle absolutniho signed prispevku |kWh| v aktualnim selection setu. Share % je vzdy vztazen k absolutnimu souctu vsech measured contributoru."
        };
    }

    private static string BuildDominantSourceSummary(
        IReadOnlyList<CuratedSelectionContributionItem> measuredContributors,
        IReadOnlyList<CuratedSelectionTopContributorItem> topContributors,
        CuratedAggregateEnergyProfile energyProfile)
    {
        if (topContributors.Count == 0)
        {
            return "A dominant source cannot be determined because the selection has no measured contributors with data.";
        }

        var dominant = topContributors[0];
        if (energyProfile == CuratedAggregateEnergyProfile.MixedSigned)
        {
            var dominantLoad = measuredContributors
                .Where(item => item.IntervalEnergyKwh > 0)
                .OrderByDescending(item => Math.Abs(item.IntervalEnergyKwh))
                .FirstOrDefault();
            var dominantGeneration = measuredContributors
                .Where(item => item.IntervalEnergyKwh < 0)
                .OrderByDescending(item => Math.Abs(item.IntervalEnergyKwh))
                .FirstOrDefault();

            if (dominantLoad is not null && dominantGeneration is not null)
            {
                return $"Dominant load component: {dominantLoad.Label}. Largest net-balance reduction: {dominantGeneration.Label}.";
            }
        }

        return dominant.Direction switch
        {
            CuratedContributionDirection.IncreasesLoad => $"The dominant component in the selection is {dominant.Label}, which increases load.",
            CuratedContributionDirection.ReducesNetBalance => $"The contributor that reduces the net balance the most is {dominant.Label} (generation/export).",
            _ => $"The dominant contributor is {dominant.Label}."
        };
    }

    private static CuratedSelectionSourceMapSummary BuildSourceMapSummary(
        IReadOnlyList<string> includedNodeKeys,
        IReadOnlyList<string> unsupportedNodeKeys,
        IReadOnlyList<string> noDataNodeKeys,
        IReadOnlyList<string> contextOnlyNodeKeys,
        IReadOnlyDictionary<string, string> labelsByNodeKey)
    {
        static string ResolveLabel(string nodeKey, IReadOnlyDictionary<string, string> labelMap)
        {
            return labelMap.TryGetValue(nodeKey, out var label) && !string.IsNullOrWhiteSpace(label)
                ? label
                : nodeKey;
        }

        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var items = new List<CuratedSelectionSourceMapItem>();

        void AddItems(IEnumerable<string> keys, CuratedSelectionSourceMapCategory category)
        {
            foreach (var nodeKey in keys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(key => ResolveLabel(key, labelsByNodeKey), StringComparer.CurrentCultureIgnoreCase))
            {
                if (!added.Add(nodeKey))
                {
                    continue;
                }

                items.Add(new CuratedSelectionSourceMapItem
                {
                    NodeKey = nodeKey,
                    Label = ResolveLabel(nodeKey, labelsByNodeKey),
                    Category = category,
                    CategoryLabel = ResolveSourceMapCategoryLabel(category)
                });
            }
        }

        AddItems(includedNodeKeys, CuratedSelectionSourceMapCategory.IncludedMeasured);
        AddItems(noDataNodeKeys, CuratedSelectionSourceMapCategory.NoData);
        AddItems(unsupportedNodeKeys, CuratedSelectionSourceMapCategory.Unsupported);
        AddItems(contextOnlyNodeKeys, CuratedSelectionSourceMapCategory.ContextOnlyExcluded);

        var summary = items.Count == 0
            ? "Source map is empty because the selection is empty or has no analytical contributors."
            : $"Included measured: {includedNodeKeys.Count}. No-data: {noDataNodeKeys.Count}. Unsupported: {unsupportedNodeKeys.Count}. Context-only/excluded: {contextOnlyNodeKeys.Count}.";

        return new CuratedSelectionSourceMapSummary
        {
            Items = items,
            IncludedMeasuredCount = includedNodeKeys.Count,
            UnsupportedCount = unsupportedNodeKeys.Count,
            NoDataCount = noDataNodeKeys.Count,
            ContextOnlyCount = contextOnlyNodeKeys.Count,
            Summary = summary
        };
    }

    private static CuratedContributionDirection ResolveContributionDirection(double intervalEnergyKwh)
    {
        if (intervalEnergyKwh > 0)
        {
            return CuratedContributionDirection.IncreasesLoad;
        }

        if (intervalEnergyKwh < 0)
        {
            return CuratedContributionDirection.ReducesNetBalance;
        }

        return CuratedContributionDirection.Neutral;
    }

    private static string ResolveSourceMapCategoryLabel(CuratedSelectionSourceMapCategory category)
    {
        return category switch
        {
            CuratedSelectionSourceMapCategory.IncludedMeasured => "included measured",
            CuratedSelectionSourceMapCategory.NoData => "no-data",
            CuratedSelectionSourceMapCategory.ContextOnlyExcluded => "context-only/excluded",
            _ => "unsupported"
        };
    }

    private static CuratedEnergyContributionRole ResolveContributionRole(double intervalEnergyKwh)
    {
        if (intervalEnergyKwh < 0)
        {
            return CuratedEnergyContributionRole.Generation;
        }

        if (intervalEnergyKwh > 0)
        {
            return CuratedEnergyContributionRole.Consumption;
        }

        return CuratedEnergyContributionRole.Neutral;
    }

    private static CuratedAggregateEnergyProfile ResolveAggregateEnergyProfile(bool hasPositiveContributions, bool hasNegativeContributions)
    {
        if (hasPositiveContributions && hasNegativeContributions)
        {
            return CuratedAggregateEnergyProfile.MixedSigned;
        }

        if (hasNegativeContributions)
        {
            return CuratedAggregateEnergyProfile.GenerationOnly;
        }

        if (hasPositiveContributions)
        {
            return CuratedAggregateEnergyProfile.ConsumptionOnly;
        }

        return CuratedAggregateEnergyProfile.Neutral;
    }

    private static (double HeadlineValueKwh, string HeadlineLabel, string HeadlineDescription, bool IsNetHeadline) ResolveHeadlineSemantics(
        CuratedAggregateEnergyProfile energyProfile,
        double totalConsumptionKwh,
        double totalGenerationKwh,
        double netEnergyKwh)
    {
        return energyProfile switch
        {
            CuratedAggregateEnergyProfile.MixedSigned =>
                (netEnergyKwh, "Net energy balance", "The selection combines consumption and generation, so the headline KPI represents the net result.", true),
            CuratedAggregateEnergyProfile.GenerationOnly =>
                (totalGenerationKwh, "Total generated energy", "The selection is generation-only/export-oriented, so the net balance is negative.", false),
            CuratedAggregateEnergyProfile.ConsumptionOnly =>
                (totalConsumptionKwh, "Total consumed energy", "The selection is consumption-only, so the net balance matches consumption.", false),
            _ =>
                (netEnergyKwh, "Net energy balance", "The selection has no meaningful energy contribution in the interval.", true)
        };
    }

    private static IReadOnlyList<CuratedNodeTimeSeriesPoint> SumTimeSeriesByTimestamp(IEnumerable<IReadOnlyList<CuratedNodeTimeSeriesPoint>> seriesCollection)
    {
        var sums = new SortedDictionary<DateTime, double>();

        foreach (var series in seriesCollection)
        {
            foreach (var point in series)
            {
                if (sums.TryGetValue(point.TimestampUtc, out var existing))
                {
                    sums[point.TimestampUtc] = existing + point.Value;
                }
                else
                {
                    sums[point.TimestampUtc] = point.Value;
                }
            }
        }

        return sums
            .Select(x => new CuratedNodeTimeSeriesPoint
            {
                TimestampUtc = x.Key,
                Value = x.Value
            })
            .ToList();
    }

    private async Task<CuratedNodeTimeSeriesResult> ParseCuratedTimeSeriesAsync(
        string nodeKey,
        string filePath,
        CuratedNodeSource source,
        DateTime from,
        DateTime to,
        CuratedNodeTimeSeriesMode mode,
        bool includeBaselineOverlay,
        CancellationToken ct)
    {
        var unit = ResolveTimeSeriesUnit(source);
        var granularity = ResolveTimeSeriesGranularity(from, to, source, mode);
        var baselineCandidates = includeBaselineOverlay && source.SupportsDeviation && to > from
            ? BuildBaselineCandidates(from, to)
            : [];

        var minRelevantFrom = from;
        var maxRelevantTo = to;
        if (baselineCandidates.Count > 0)
        {
            var baselineMin = baselineCandidates.Min(candidate => candidate.From);
            var baselineMax = baselineCandidates.Max(candidate => candidate.To);

            if (baselineMin < minRelevantFrom)
            {
                minRelevantFrom = baselineMin;
            }

            if (baselineMax > maxRelevantTo)
            {
                maxRelevantTo = baselineMax;
            }
        }

        CuratedNodeTimeSeriesResult CreateResult(
            IReadOnlyList<CuratedNodeTimeSeriesPoint> points,
            string? noDataMessage,
            IReadOnlyList<CuratedNodeTimeSeriesPoint> baselinePoints,
            string? baselineOverlayMessage)
        {
            return new CuratedNodeTimeSeriesResult
            {
                NodeKey = nodeKey,
                Title = source.Title,
                Unit = unit,
                YAxisLabel = ResolveTimeSeriesYAxisLabel(source),
                SeriesSemantics = source.SeriesSemantics,
                UsesDerivedIntervalSeries = source.SeriesSemantics == FacilitySignalSeriesSemantics.CumulativeCounter,
                SeriesStatusMessage = ResolveSeriesStatusMessage(source, granularity.Granularity),
                Granularity = granularity.Granularity,
                GranularityLabel = granularity.Label,
                AggregationMethod = granularity.AggregationMethod,
                InterpretationNote = ResolveTimeSeriesInterpretationNote(source, granularity),
                RequestedMode = granularity.RequestedMode,
                RequestedModeLabel = granularity.RequestedModeLabel,
                BaselineOverlayRequested = includeBaselineOverlay,
                BaselineOverlayAvailable = baselinePoints.Count > 0,
                BaselineOverlayMessage = baselineOverlayMessage,
                BaselinePoints = baselinePoints,
                NoDataMessage = noDataMessage,
                Points = points
            };
        }

        try
        {
            using var reader = OpenCsvReader(filePath);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(headerLine))
            {
                return CreateResult(
                    [],
                    "The reduced source is empty.",
                    [],
                    includeBaselineOverlay ? "Baseline overlay is unavailable because the reduced source is empty." : null);
            }

            var headers = headerLine.Split(',');
            int colIndex = Array.FindIndex(headers, h => h.Trim().Equals(source.ColumnName, StringComparison.OrdinalIgnoreCase));
            int timeColIndex = Array.FindIndex(headers, h =>
                h.Trim().Equals("datetime_utc", StringComparison.OrdinalIgnoreCase) ||
                h.Trim().Equals("timestamp", StringComparison.OrdinalIgnoreCase) ||
                h.Trim().Equals("time", StringComparison.OrdinalIgnoreCase));

            if (colIndex == -1)
            {
                return CreateResult(
                    [],
                    "The reduced source does not contain the expected column for the selected node.",
                    [],
                    includeBaselineOverlay ? "Baseline overlay is unavailable because the reduced source does not contain the expected column." : null);
            }

            if (timeColIndex == -1)
            {
                timeColIndex = 0;
            }

            var rawPoints = new List<CuratedNodeTimeSeriesPoint>();
            var bucketStats = new SortedDictionary<DateTime, RunningStats>();
            var baselineBucketStatsByCandidate = baselineCandidates.ToDictionary(
                candidate => candidate,
                _ => new SortedDictionary<DateTime, RunningStats>());
            var baselineEnergyStatsByCandidate = baselineCandidates.ToDictionary(
                candidate => candidate,
                _ => new RunningStats());

            var currentIntervalSamples = 0;
            var sampleStepHours = DefaultPowerSampleStepHours;
            DateTime? previousTimestamp = null;
            double? previousCounterValue = null;

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                var cols = line.Split(',');
                if (cols.Length <= Math.Max(colIndex, timeColIndex))
                {
                    continue;
                }

                if (!TryParseTimestamp(cols[timeColIndex], out var timestamp))
                {
                    continue;
                }

                UpdateSampleStepHours(previousTimestamp, timestamp, ref sampleStepHours);
                previousTimestamp = timestamp;

                if (!double.TryParse(cols[colIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var rawValue))
                {
                    continue;
                }

                var hasSeriesValue = TryResolveSeriesSampleValues(
                    rawValue,
                    source,
                    sampleStepHours,
                    ref previousCounterValue,
                    out var statsValue,
                    out var aggregationValue);

                if (timestamp < minRelevantFrom)
                {
                    continue;
                }

                if (timestamp >= maxRelevantTo)
                {
                    break;
                }

                if (!hasSeriesValue)
                {
                    continue;
                }

                if (timestamp >= from && timestamp < to)
                {
                    currentIntervalSamples++;

                    if (granularity.Granularity == CuratedNodeTimeSeriesGranularity.Raw15Min)
                    {
                        rawPoints.Add(new CuratedNodeTimeSeriesPoint
                        {
                            TimestampUtc = timestamp,
                            Value = statsValue
                        });
                    }
                    else
                    {
                        var bucketStart = GetBucketStartUtc(timestamp, granularity.Granularity);
                        if (!bucketStats.TryGetValue(bucketStart, out var stats))
                        {
                            stats = new RunningStats();
                            bucketStats[bucketStart] = stats;
                        }

                        stats.Add(statsValue);
                    }
                }

                if (baselineCandidates.Count == 0)
                {
                    continue;
                }

                foreach (var candidate in baselineCandidates)
                {
                    if (timestamp < candidate.From || timestamp >= candidate.To)
                    {
                        continue;
                    }

                    baselineEnergyStatsByCandidate[candidate].Add(aggregationValue);

                    var alignedTimestamp = from + (timestamp - candidate.From);
                    if (alignedTimestamp < from || alignedTimestamp >= to)
                    {
                        continue;
                    }

                    var baselineBucketStart = GetBucketStartUtc(alignedTimestamp, granularity.Granularity);
                    var candidateBuckets = baselineBucketStatsByCandidate[candidate];
                    if (!candidateBuckets.TryGetValue(baselineBucketStart, out var baselineStats))
                    {
                        baselineStats = new RunningStats();
                        candidateBuckets[baselineBucketStart] = baselineStats;
                    }

                    baselineStats.Add(statsValue);
                }
            }

            IReadOnlyList<CuratedNodeTimeSeriesPoint> points = granularity.Granularity == CuratedNodeTimeSeriesGranularity.Raw15Min
                ? rawPoints
                : bucketStats
                    .Where(x => x.Value.Count > 0)
                    .Select(x => new CuratedNodeTimeSeriesPoint
                    {
                        TimestampUtc = x.Key,
                        Value = ResolveBucketValue(x.Value, source)
                    })
                    .ToList();

            IReadOnlyList<CuratedNodeTimeSeriesPoint> baselinePoints = [];
            string? baselineOverlayMessage = null;

            if (includeBaselineOverlay)
            {
                if (!source.SupportsDeviation)
                {
                    baselineOverlayMessage = "Baseline overlay is not supported for this node.";
                }
                else if (baselineCandidates.Count == 0)
                {
                    baselineOverlayMessage = "Baseline overlay is unavailable because reference baseline windows are missing.";
                }
                else if (currentIntervalSamples == 0)
                {
                    baselineOverlayMessage = "Baseline overlay cannot be calculated because the analysis window has no current samples.";
                }
                else
                {
                    var minimumReferenceSamples = Math.Max(2, (int)Math.Round(currentIntervalSamples * MinimumReferenceCoverageRatio));

                    var samePeriodReferenceAggregates = baselineCandidates
                        .Where(candidate => candidate.Kind == BaselineReferenceKind.SamePeriodPreviousYear)
                        .Select(candidate => new BaselineReferenceAggregate(candidate, baselineEnergyStatsByCandidate[candidate].Sum))
                        .Where(aggregate => baselineEnergyStatsByCandidate[aggregate.Candidate].Count >= minimumReferenceSamples)
                        .ToList();

                    var recentComparableReferenceAggregates = baselineCandidates
                        .Where(candidate => candidate.Kind == BaselineReferenceKind.RecentComparablePeriod)
                        .Select(candidate => new BaselineReferenceAggregate(candidate, baselineEnergyStatsByCandidate[candidate].Sum))
                        .Where(aggregate => baselineEnergyStatsByCandidate[aggregate.Candidate].Count >= minimumReferenceSamples)
                        .ToList();

                    var selectedBaseline = SelectBaselineValue(samePeriodReferenceAggregates, recentComparableReferenceAggregates);
                    if (selectedBaseline is null)
                    {
                        baselineOverlayMessage = "Baseline overlay is unavailable because the reference baseline windows do not have sufficient data coverage.";
                    }
                    else
                    {
                        baselinePoints = BuildBaselineOverlaySeries(selectedBaseline, baselineBucketStatsByCandidate);
                        if (baselinePoints.Count == 0)
                        {
                            baselineOverlayMessage = "Baseline overlay is unavailable because the reference series has no points for the selected granularity.";
                        }
                    }
                }
            }

            return CreateResult(
                points,
                points.Count == 0 ? "No time-series points are available for the selected interval." : null,
                baselinePoints,
                baselineOverlayMessage);
        }
        catch
        {
            return CreateResult(
                [],
                "The reduced source for the time series could not be loaded.",
                [],
                includeBaselineOverlay ? "Baseline overlay is unavailable because the reduced source could not be loaded." : null);
        }
    }

    private static IReadOnlyList<CuratedNodeTimeSeriesPoint> BuildBaselineOverlaySeries(
        BaselineSelection selectedBaseline,
        IReadOnlyDictionary<BaselineCandidate, SortedDictionary<DateTime, RunningStats>> baselineBucketStatsByCandidate)
    {
        var selectedCandidates = selectedBaseline.ReferenceCandidates
            .Where(candidate => baselineBucketStatsByCandidate.ContainsKey(candidate))
            .ToList();

        if (selectedCandidates.Count == 0)
        {
            return [];
        }

        var seriesByCandidate = selectedCandidates.ToDictionary(
            candidate => candidate,
            candidate => baselineBucketStatsByCandidate[candidate]
                .Where(x => x.Value.Count > 0)
                .ToDictionary(x => x.Key, x => x.Value.Sum / x.Value.Count));

        var samePeriodCandidates = selectedCandidates
            .Where(candidate => candidate.Kind == BaselineReferenceKind.SamePeriodPreviousYear)
            .ToList();
        var recentComparableCandidates = selectedCandidates
            .Where(candidate => candidate.Kind == BaselineReferenceKind.RecentComparablePeriod)
            .ToList();

        if (selectedBaseline.StrategyDescription.StartsWith("Hybrid:", StringComparison.OrdinalIgnoreCase)
            && samePeriodCandidates.Count == 1
            && recentComparableCandidates.Count > 0)
        {
            var sameSeries = seriesByCandidate[samePeriodCandidates[0]];
            var recentBuckets = new Dictionary<DateTime, List<double>>();

            foreach (var candidate in recentComparableCandidates)
            {
                foreach (var point in seriesByCandidate[candidate])
                {
                    if (!recentBuckets.TryGetValue(point.Key, out var values))
                    {
                        values = [];
                        recentBuckets[point.Key] = values;
                    }

                    values.Add(point.Value);
                }
            }

            var allBuckets = sameSeries.Keys
                .Union(recentBuckets.Keys)
                .OrderBy(x => x)
                .ToList();

            return allBuckets
                .Select(bucket =>
                {
                    var hasSame = sameSeries.TryGetValue(bucket, out var sameValue);
                    recentBuckets.TryGetValue(bucket, out var recentValues);

                    if (!hasSame && (recentValues is null || recentValues.Count == 0))
                    {
                        return null;
                    }

                    if (hasSame && recentValues is { Count: > 0 })
                    {
                        return new CuratedNodeTimeSeriesPoint
                        {
                            TimestampUtc = bucket,
                            Value = (sameValue * 0.70) + (Median(recentValues) * 0.30)
                        };
                    }

                    if (hasSame)
                    {
                        return new CuratedNodeTimeSeriesPoint
                        {
                            TimestampUtc = bucket,
                            Value = sameValue
                        };
                    }

                    return new CuratedNodeTimeSeriesPoint
                    {
                        TimestampUtc = bucket,
                        Value = Median(recentValues!)
                    };
                })
                .Where(point => point is not null)
                .Cast<CuratedNodeTimeSeriesPoint>()
                .ToList();
        }

        var bucketValues = new Dictionary<DateTime, List<double>>();
        foreach (var candidate in selectedCandidates)
        {
            foreach (var point in seriesByCandidate[candidate])
            {
                if (!bucketValues.TryGetValue(point.Key, out var values))
                {
                    values = [];
                    bucketValues[point.Key] = values;
                }

                values.Add(point.Value);
            }
        }

        return bucketValues
            .OrderBy(x => x.Key)
            .Select(x => new CuratedNodeTimeSeriesPoint
            {
                TimestampUtc = x.Key,
                Value = Median(x.Value)
            })
            .ToList();
    }

    private async Task<CuratedNodeSummary?> ParseCsvColumnAsync(string filePath, CuratedNodeSource source, DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            using var reader = OpenCsvReader(filePath);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(headerLine)) return null;

            var headers = headerLine.Split(',');
            int colIndex = Array.FindIndex(headers, h => h.Trim().Equals(source.ColumnName, StringComparison.OrdinalIgnoreCase));
            int timeColIndex = Array.FindIndex(headers, h => 
                h.Trim().Equals("datetime_utc", StringComparison.OrdinalIgnoreCase) || 
                h.Trim().Equals("timestamp", StringComparison.OrdinalIgnoreCase) || 
                h.Trim().Equals("time", StringComparison.OrdinalIgnoreCase));
            
            if (colIndex == -1) return null;
            if (timeColIndex == -1) timeColIndex = 0; // fallback to first column

            double aggregateSum = 0;
            double statsSum = 0;
            int count = 0;
            double min = double.MaxValue;
            double max = double.MinValue;
            var sampleStepHours = DefaultPowerSampleStepHours;
            DateTime? previousTimestamp = null;
            double? previousCounterValue = null;

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                var cols = line.Split(',');
                if (cols.Length > timeColIndex && cols.Length > colIndex)
                {
                    if (TryParseTimestamp(cols[timeColIndex], out var timestamp))
                    {
                        UpdateSampleStepHours(previousTimestamp, timestamp, ref sampleStepHours);
                        previousTimestamp = timestamp;

                        if (!double.TryParse(cols[colIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                        {
                            continue;
                        }

                        var hasSeriesValue = TryResolveSeriesSampleValues(
                            val,
                            source,
                            sampleStepHours,
                            ref previousCounterValue,
                            out var statsValue,
                            out var aggregateValue);

                        if (timestamp >= from && timestamp < to && hasSeriesValue)
                        {
                            aggregateSum += aggregateValue;
                            statsSum += statsValue;
                            count++;
                            if (statsValue < min) min = statsValue;
                            if (statsValue > max) max = statsValue;
                        }
                        
                        // ZastavĂ­ ÄŤtenĂ­ pokud jsme chronologicky pĹ™esĂˇhli zkoumanĂ© obdobĂ­ (zlepĹˇuje vĂ˝kon pro starĹˇĂ­ data)
                        if (timestamp >= to)
                        {
                            break;
                        }
                    }
                }
            }

            if (count == 0) return null;

            return new CuratedNodeSummary
            {
                Title = source.Title,
                TotalSum = aggregateSum,
                Average = statsSum / count,
                Min = min,
                Max = max,
                DataPoints = count,
                Unit = source.Unit,
                SummaryLabel = source.SummaryLabel,
                StatsUnit = source.StatsUnit,
                StatsLabel = source.StatsLabel
            };
        }
        catch
        {
            return null; // PĹ™i file I/O problĂ©mu prostÄ› fallbackujeme do NO-DATA stavu
        }
    }
}
