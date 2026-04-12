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
    public CuratedNodeTimeSeriesGranularity Granularity { get; init; } = CuratedNodeTimeSeriesGranularity.Raw15Min;
    public string GranularityLabel { get; init; } = "15min detail";
    public string AggregationMethod { get; init; } = "Bez agregace (raw řada).";
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
    public string AggregationMethod { get; init; } = "Bez agregace (raw řada).";
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
    public CuratedSelectionPeakAnalysisSummary PeakAnalysis { get; init; } = new();
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
    public string HeadlineLabel { get; init; } = "Netto bilance energie";
    public string HeadlineDescription { get; init; } = string.Empty;
    public bool IsNetHeadline { get; init; }
    public IReadOnlyList<string> SupportedNodeKeys { get; init; } = [];
    public IReadOnlyList<string> UnsupportedNodeKeys { get; init; } = [];
    public IReadOnlyList<string> ContextOnlyNodeKeys { get; init; } = [];
    public IReadOnlyList<string> NoDataNodeKeys { get; init; } = [];
    public IReadOnlyList<string> IncludedNodeKeys { get; init; } = [];
    public string? Message { get; init; }
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
    public CuratedSelectionPeakEvent DemandPeak { get; init; } = new() { Label = "Peak demand" };
    public CuratedSelectionPeakEvent GenerationPeak { get; init; } = new() { Label = "Peak generation/export" };
    public CuratedSelectionPeakEvent NetAbsolutePeak { get; init; } = new() { Label = "Peak net absolute event" };
    public double? TypicalMagnitudeKw { get; init; }
    public double? SignificanceRatio { get; init; }
    public CuratedPeakSignificanceLevel SignificanceLevel { get; init; } = CuratedPeakSignificanceLevel.Low;
    public string Summary { get; init; } = string.Empty;
    public string Methodology { get; init; } = string.Empty;
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
    private const string BaselineMethodology = "Analysis window je vždy přímo vybraný interval uživatele. Baseline strategy je určena separátně: priorita je stejné období v minulých letech, fallback jsou předchozí srovnatelná období stejné délky. U řad s příponou P se před součtem převádí výkon na energii podle kroku časové řady (inferovaný krok, fallback 15 min).";
    private const double DefaultPowerSampleStepHours = 0.25;
    private const int MaxHistoricalYearsForBaseline = 3;
    private const int RecentComparableWindowsForBaseline = 4;
    private const double MinimumReferenceCoverageRatio = 0.60;
    private const double WeatherExplanationDeltaThresholdC = 0.8;
    private static readonly TimeSpan RawTimeSeriesThreshold = TimeSpan.FromDays(7);
    private static readonly TimeSpan HourlyTimeSeriesThreshold = TimeSpan.FromDays(45);
    private static readonly HashSet<string> ComparePreviewSupportedNodeKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "heating_main",
        "cooling_main",
        "pv_main",
        "chp_main"
    };

    private readonly IKpiService _kpiService;
    private readonly IWebHostEnvironment _env;
    private readonly ConcurrentDictionary<string, DateTime> _maxTimestampCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, (DateTime MinUtc, DateTime MaxUtc)> _timeDomainCache = new(StringComparer.OrdinalIgnoreCase);

    public NodeAnalyticsPreviewService(IKpiService kpiService, IWebHostEnvironment env)
    {
        _kpiService = kpiService;
        _env = env;
    }

    public async Task<MeterKpiResult?> GetPreviewDataAsync(string meterUrn, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var query = new KpiQuery(meterUrn, from, to);
        var result = await _kpiService.CalculateBasicKpiAsync(query, ct);

        // Pokud nemáme žádná data, vracíme null
        if (result.RecordCount == 0)
            return null;

        return result;
    }

    public async Task<CuratedNodeSummary?> GetCuratedSummaryAsync(string nodeKey, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var source = ResolveCuratedNodeSource(nodeKey);
        if (source is null)
        {
            return null;
        }

        var filePath = ResolveCuratedFilePath(source.FileName);
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
    {
        var source = ResolveCuratedNodeSource(nodeKey);
        if (source is null)
        {
            return null;
        }

        var filePath = ResolveCuratedFilePath(source.FileName);
        if (filePath is null)
        {
            var granularity = ResolveTimeSeriesGranularity(from, to, source, mode);
            return new CuratedNodeTimeSeriesResult
            {
                NodeKey = nodeKey,
                Title = source.Title,
                Unit = ResolveTimeSeriesUnit(source),
                YAxisLabel = ResolveTimeSeriesYAxisLabel(source),
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
                        ? "Baseline overlay není dostupný: chybí lokální reduced source."
                        : "Baseline overlay není pro tento uzel podporován.")
                    : null,
                NoDataMessage = "Chybí lokální reduced source pro vykreslení časové řady."
            };
        }

        return await ParseCuratedTimeSeriesAsync(nodeKey, filePath, source, from, to, mode, includeBaselineOverlay, ct);
    }

    public async Task<CuratedSelectionAggregateOverviewResult> GetCuratedSelectionAggregateOverviewAsync(
        IEnumerable<string> selectedNodeKeys,
        DateTime from,
        DateTime to,
        CuratedNodeTimeSeriesMode mode = CuratedNodeTimeSeriesMode.Auto,
        bool includeBaselineOverlay = false,
        CancellationToken ct = default)
    {
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
                    Summary = "Load profile není dostupný, protože selection set neobsahuje podporované energetické uzly.",
                    Methodology = "Daily profile v1 používá transparentní agregaci aggregate výkonové řady Selection Setu (hour-of-day average nebo fallback interval snapshot).",
                    DifferenceFromForecast = "Forecast je predikce budoucího průběhu, load profile je popis typického historického chování.",
                    DifferenceFromMainChart = "Hlavní chart zobrazuje průběh konkrétního intervalu; load profile agreguje opakující se pattern napříč intervalem."
                },
                PeakAnalysis = new CuratedSelectionPeakAnalysisSummary
                {
                    IsAvailable = false,
                    Summary = "Peak analysis není dostupná bez podporovaných uzlů.",
                    Methodology = "Peak analysis v1 vyhodnocuje peak demand, peak generation/export a peak net absolute event z aggregate výkonové řady."
                },
                OperatingRegime = new CuratedSelectionOperatingRegimeSummary
                {
                    IsAvailable = false,
                    Summary = "Operating regime summary není dostupná bez aggregate časové řady.",
                    Methodology = "Operating regime v1 používá transparentní heuristiky nad aggregate výkonem: baseload proxy, peak-to-average, variabilitu a weekday/weekend signal."
                },
                EmsEvaluation = new CuratedSelectionEmsEvaluationSummary
                {
                    IsAvailable = false,
                    Summary = "EMS evaluation není dostupná: selection set neobsahuje podporované energetické uzly.",
                    Methodology = "EMS evaluation v1 staví transparentní scorecards pouze nad aggregate load profile, peak a operating regime metrikami.",
                    DistinctionNote = "Issue = datový/coverage problém, anomaly = odchylka proti baseline, inefficiency = provozní neefektivita režimu."
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
                    Summary = "Forecast není dostupný, protože selection set neobsahuje podporované energetické uzly.",
                    ForecastPrinciple = "Comparable windows v1 (transparentní)",
                    Methodology = "Forecast používá jen historická referenční okna před target intervalem; unsupported uzly se do výpočtu nezahrnují.",
                    MetricsNote = "Diagnostické metriky MAE/RMSE/Bias/WAPE se počítají pouze při dostupné forecast i actual sérii.",
                    SupportedNodeCount = 0,
                    IncludedNodeCount = 0,
                    ForecastProviderNodeCount = 0,
                    ForecastMissingNodeCount = 0,
                    UsesTargetLeakage = false,
                    Signals = ["no_supported_nodes"]
                },
                Message = "Selection set neobsahuje kompatibilní energetické uzly pro agregaci overview."
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
                : "Selection Set agregace";
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
                StatsLabel = "Agregovaný výkon"
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
                    baselineOverlayMessage = "Baseline overlay není v aggregate režimu dostupný pro žádný podporovaný uzel.";
                }
                else if (baselineProviders < timeSeries.Count)
                {
                    baselineOverlayMessage = $"Baseline overlay je agregován z {baselineProviders}/{timeSeries.Count} podporovaných uzlů.";
                }
            }

            aggregateTimeSeries = new CuratedNodeTimeSeriesResult
            {
                NodeKey = "selection_set",
                Title = supportedNodeKeys.Count == 1 ? template.Title : "Aggregate výkon Selection Setu",
                Unit = template.Unit,
                YAxisLabel = template.YAxisLabel,
                Granularity = template.Granularity,
                GranularityLabel = template.GranularityLabel,
                AggregationMethod = $"{template.AggregationMethod} Selection aggregate: suma výkonů podporovaných uzlů v každém časovém bodě.",
                InterpretationNote = ResolveSelectionAggregateInterpretationNote(template.Granularity, energyProfile),
                RequestedMode = template.RequestedMode,
                RequestedModeLabel = template.RequestedModeLabel,
                BaselineOverlayRequested = includeBaselineOverlay,
                BaselineOverlayAvailable = baselinePoints.Count > 0,
                BaselineOverlayMessage = baselineOverlayMessage,
                BaselinePoints = baselinePoints,
                NoDataMessage = points.Count == 0
                    ? "Pro podporované uzly selection setu nejsou v intervalu dostupné body časové řady."
                    : null,
                Points = points
            };
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
            messageParts.Add($"Ignorováno nepodporovaných uzlů: {unsupportedNodeKeys.Count}");
        }

        if (contextOnlyNodeKeys.Count > 0)
        {
            messageParts.Add($"Context-only/excluded uzly: {contextOnlyNodeKeys.Count}");
        }

        if (noDataNodeKeys.Count > 0)
        {
            messageParts.Add($"Uzlů bez dat v intervalu: {noDataNodeKeys.Count}");
        }

        var message = messageParts.Count > 0
            ? string.Join(". ", messageParts) + "."
            : null;

        var operationalHealth = BuildOperationalHealthSummary(
            coverage,
            energyProfile,
            deviationSummaries,
            aggregateTimeSeries);
        var loadProfile = BuildLoadProfileSummary(
            aggregateTimeSeries,
            from,
            to,
            energyProfile,
            includedNodeKeys.Count);
        var peakAnalysis = BuildPeakAnalysisSummary(aggregateTimeSeries, energyProfile);
        var operatingRegime = BuildOperatingRegimeSummary(aggregateTimeSeries, energyProfile);
        var emsEvaluation = BuildEmsEvaluationSummary(
            aggregateTimeSeries,
            loadProfile,
            peakAnalysis,
            operatingRegime,
            energyProfile,
            totalConsumptionKwh,
            totalGenerationKwh,
            netEnergyKwh);

        return new CuratedSelectionAggregateOverviewResult
        {
            Summary = aggregateSummary,
            TimeSeries = aggregateTimeSeries,
            LoadProfile = loadProfile,
            PeakAnalysis = peakAnalysis,
            OperatingRegime = operatingRegime,
            EmsEvaluation = emsEvaluation,
            Breakdown = breakdown,
            RoleBreakdown = roleBreakdown,
            Disaggregation = disaggregation,
            ContributionIntelligence = contributionIntelligence,
            SourceMap = sourceMap,
            Coverage = coverage,
            OperationalHealth = operationalHealth,
            ForecastCompareTimeSeries = forecastCompareTimeSeries,
            ForecastDiagnostics = forecastDiagnostics,
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

    private static (CuratedNodeCompareTimeSeriesResult? CompareTimeSeries, CuratedSelectionForecastDiagnosticsSummary Diagnostics) BuildSelectionForecastOutputs(
        CuratedNodeTimeSeriesResult? aggregateActualTimeSeries,
        IReadOnlyList<CuratedNodeTimeSeriesPoint> aggregateForecastPoints,
        CuratedSelectionCoverageSummary coverage,
        CuratedAggregateEnergyProfile energyProfile,
        int forecastProviderNodeCount)
    {
        var baseDiagnostics = new CuratedSelectionForecastDiagnosticsSummary
        {
            ForecastPrinciple = "Comparable windows v1 (transparentní)",
            Methodology = "Forecast je konstruován z historických referenčních oken (stejné období v minulých letech + recent comparable intervaly). Do forecastu vstupují pouze data dostupná před target intervalem; leakage z target okna není použit.",
            MetricsNote = "MAE, RMSE a Bias jsou počítány nad podepsaným výkonem (kW). WAPE používá sumu absolutních actual hodnot, takže je robustní i pro mixed-sign selection.",
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
                Summary = "Forecast vs actual nelze vyhodnotit, protože aggregate actual řada nemá body.",
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
                Summary = "Forecast reference není pro aktuální interval dostupná (nedostatečné historical windows).",
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
                Summary = "Actual a forecast řada nemají časový překryv po agregaci.",
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
            CuratedSelectionForecastStatus.Stable => "Forecast sedí stabilně, rozdíl actual vs forecast je nízký.",
            CuratedSelectionForecastStatus.Watch => "Forecast je použitelný, ale odchylka actual vs forecast vyžaduje pozornost.",
            CuratedSelectionForecastStatus.PoorFit => "Forecast má slabou shodu s actual řadou v aktuálním intervalu.",
            CuratedSelectionForecastStatus.LimitedData => "Forecast je jen orientační: nízký překryv nebo málo aligned bodů.",
            _ => "Forecast není dostupný."
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
            compareExcludedMessages.Add($"Forecast reference nebyla dostupná pro {baseDiagnostics.ForecastMissingNodeCount}/{Math.Max(1, baseDiagnostics.IncludedNodeCount)} analyticky zahrnutých uzlů.");
        }

        var compareData = new CuratedNodeCompareTimeSeriesResult
        {
            PrimaryNodeKey = "selection_set",
            Title = "Forecast vs Actual",
            Unit = aggregateActualTimeSeries.Unit,
            YAxisLabel = aggregateActualTimeSeries.YAxisLabel,
            Granularity = aggregateActualTimeSeries.Granularity,
            GranularityLabel = aggregateActualTimeSeries.GranularityLabel,
            AggregationMethod = "Forecast v1: transparent comparable windows (historická okna před target intervalem). Actual i forecast jsou agregované přes Selection Set bez unsupported uzlů.",
            InterpretationNote = "Forecast je predikce očekávaného průběhu, zatímco baseline/deviation vrstva zůstává samostatný referenční mechanismus pro alerting.",
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
            ExcludedNodeMessages = compareExcludedMessages,
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
            CuratedSelectionAnomalyStatus.DataIssue when hasNoSupportedNodes => "Selection je mimo podporované analytické uzly.",
            CuratedSelectionAnomalyStatus.DataIssue when hasNoIncludedNodes => "Podporované uzly nemají v intervalu použitelná data.",
            CuratedSelectionAnomalyStatus.DataIssue => "Datová kvalita je nízká, analytický výstup má omezenou důvěryhodnost.",
            CuratedSelectionAnomalyStatus.Suspicious when highDeviationCount > 0 => "Detekována silná deviation v části selection setu.",
            CuratedSelectionAnomalyStatus.Suspicious when hasAbruptShift => "Agregovaná řada obsahuje náhlý skok oproti běžné úrovni.",
            CuratedSelectionAnomalyStatus.Suspicious => "Kombinace signálů indikuje podezřelé chování.",
            CuratedSelectionAnomalyStatus.Attention when elevatedDeviationCount > 0 => "Výběr vykazuje zvýšené deviation signály.",
            CuratedSelectionAnomalyStatus.Attention when hasWeakCoverage => "Coverage je oslabené, interpretujte výsledek opatrně.",
            CuratedSelectionAnomalyStatus.Attention when hasMixedSignedSelection => "Selection kombinuje spotřebu i výrobu, netto vyžaduje opatrnou interpretaci.",
            CuratedSelectionAnomalyStatus.Attention => "Výběr vyžaduje zvýšenou pozornost.",
            _ => "Bez zjevné anomálie, deviation i coverage jsou stabilní."
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
                Summary = "Load profile není dostupný: aggregate časová řada nemá dost bodů.",
                Methodology = "Daily profile v1 používá average by hour-of-day nad aggregate výkonovou řadou Selection Setu; při krátkém intervalu přechází na transparentní interval snapshot.",
                DifferenceFromForecast = "Forecast je predikce očekávaného budoucího průběhu, load profile popisuje typický historický pattern.",
                DifferenceFromMainChart = "Hlavní chart ukazuje konkrétní průběh v čase, load profile ukazuje opakující se tvar dne v agregovaném pohledu."
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
                    Summary = "Load profile není dostupný: po hourly bucketizaci nezbyly žádné validní body.",
                    Methodology = "Daily profile v1 používá average by hour-of-day nad aggregate výkonovou řadou Selection Setu; při krátkém intervalu přechází na transparentní interval snapshot.",
                    DifferenceFromForecast = "Forecast je predikce očekávaného budoucího průběhu, load profile popisuje typický historický pattern.",
                    DifferenceFromMainChart = "Hlavní chart ukazuje konkrétní průběh v čase, load profile ukazuje opakující se tvar dne v agregovaném pohledu."
                };
            }

            var peakBucket = hourlyBuckets
                .OrderByDescending(bucket => Math.Abs(bucket.AverageKw))
                .First();

            var summary = hasMixedSigns
                ? $"Daily profile (hour-of-day) z {distinctDayCount} dní a {orderedPoints.Count} bodů ({selectionScope}). Mixed-sign semantika je zachována (+ load, - generation/export)."
                : $"Daily profile (hour-of-day) z {distinctDayCount} dní a {orderedPoints.Count} bodů ({selectionScope}). Nejvýraznější hodinový bucket: {peakBucket.Label}.";

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
                Methodology = "Profil je transparentní průměr aggregate výkonu (kW) podle hodiny dne nad zvoleným intervalem. Bez black-box modelu.",
                DifferenceFromForecast = "Forecast vrstva odhaduje budoucí průběh; load profile vrstva shrnuje typické opakující se chování v rámci dne.",
                DifferenceFromMainChart = "Hlavní chart drží chronologii konkrétních bodů intervalu; profile ztrácí konkrétní datum a zachovává jen typický intradenní pattern.",
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
            Summary = $"Interval je krátký ({distinctDayCount} den/dny), proto je použit snapshot po {fallbackBucketCount} segmentech místo typického denního profilu.",
            Methodology = "Fallback profil používá průměr aggregate výkonu (kW) v rovnoměrných segmentech aktuálního intervalu; nevydává se za typický denní profil.",
            DifferenceFromForecast = "Forecast vrstva predikuje další vývoj, fallback profile je pouze transparentní strukturace aktuálního krátkého intervalu.",
            DifferenceFromMainChart = "Hlavní chart ukazuje každý bod; fallback profile zhušťuje interval do několika segmentů pro rychlé čtení provozního tvaru.",
            Buckets = snapshotBuckets
        };
    }

    private static CuratedSelectionPeakAnalysisSummary BuildPeakAnalysisSummary(
        CuratedNodeTimeSeriesResult? aggregateTimeSeries,
        CuratedAggregateEnergyProfile energyProfile)
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
                Summary = "Peak analysis není dostupná bez aggregate časové řady.",
                Methodology = "Peak analysis v1 vyhodnocuje peak demand, peak generation/export a peak net absolute event z aggregate výkonové řady Selection Setu."
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
        var demandPeak = demandCandidate is null
            ? EmptyPeak("Peak demand")
            : ToPeak("Peak demand", demandCandidate);
        var generationPeak = generationCandidate is null
            ? EmptyPeak("Peak generation/export")
            : ToPeak("Peak generation/export", generationCandidate);
        var netPeak = ToPeak("Peak net absolute event", netAbsoluteCandidate);

        var summary = energyProfile switch
        {
            CuratedAggregateEnergyProfile.ConsumptionOnly => "Selection je consumption-only: hlavní event je demand peak a jeho významnost vůči typickému loadu.",
            CuratedAggregateEnergyProfile.GenerationOnly => "Selection je generation-only: hlavní event je generation/export peak a jeho významnost.",
            CuratedAggregateEnergyProfile.MixedSigned => "Selection je mixed-sign: sledují se odděleně demand peak, generation peak i net absolute peak event.",
            _ => "Peak analysis je pouze orientační, protože selection nemá výrazné energetické body."
        };

        return new CuratedSelectionPeakAnalysisSummary
        {
            IsAvailable = true,
            HasMixedSigns = hasMixedSigns,
            DemandPeak = demandPeak,
            GenerationPeak = generationPeak,
            NetAbsolutePeak = netPeak,
            TypicalMagnitudeKw = typicalMagnitude > 0 ? typicalMagnitude : null,
            SignificanceRatio = significanceRatio,
            SignificanceLevel = significanceLevel,
            Summary = summary,
            Methodology = "Peak analysis není jen KPI min/max: explicitně mapuje peak eventy na konkrétní timestampy a porovnává jejich magnitudu s typickou |kW| úrovní aggregate řady (medián |kW|)."
        };
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
                Summary = "Operating regime summary není dostupná: aggregate řada je příliš krátká.",
                Methodology = "Operating regime v1 používá transparentní heuristiky nad aggregate výkonem: baseload proxy, peak-to-average, variabilitu a weekday/weekend signal."
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
            ? "Výběr má výrazně špičkový profil."
            : (baseload / Math.Max(0.25, averageAbs)) >= 0.75 && variability < 0.45
                ? "Výběr má stabilní baseload charakter."
                : variability >= 0.60
                    ? "Výběr má výraznou denní variabilitu."
                    : "Výběr má vyvážený provozní režim bez extrémní špičkovosti.";

        if (weekdayWeekendDeltaPercent.HasValue && Math.Abs(weekdayWeekendDeltaPercent.Value) >= 20.0)
        {
            var direction = weekdayWeekendDeltaPercent.Value > 0 ? "weekday" : "weekend";
            summary += $" Výrazný weekday/weekend rozdíl ({direction} dominantní).";
        }

        if (energyProfile == CuratedAggregateEnergyProfile.MixedSigned)
        {
            summary += " Selection je mixed-sign, metriky jsou proto počítány nad |kW|.";
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
            Methodology = "Regime heuristika v1: baseload proxy = 20. percentil |kW|, peak-to-average = max(|kW|)/avg(|kW|), variability = std(|kW|)/avg(|kW|), weekday/weekend signal = rozdíl průměrů normalizovaný avg(|kW|).",
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
                Summary = "EMS evaluation v1 není dostupná: aggregate řada je příliš krátká nebo chybí operating regime metriky.",
                Methodology = "EMS evaluation v1 používá transparentní pravidla nad load profile, peak analysis a operating regime vrstvou.",
                DistinctionNote = "Issue = datový/coverage problém, anomaly = odchylka proti baseline, inefficiency = provozní neefektivita režimu."
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
                        CuratedOperationalScorecardStatus.Issue => "Mimo aktivní hodiny přetrvává vysoký load.",
                        CuratedOperationalScorecardStatus.Watch => "Mimo aktivní hodiny je load zvýšený.",
                        _ => "Mimo aktivní hodiny load výrazně klesá."
                    },
                    Methodology = $"Průměr aggregate loadu mimo 07:00-19:00 vůči průměru v aktivních hodinách. Sample count active/off: {activeCount}/{offCount}."
                });
            }
            else
            {
                scorecards.Add(new CuratedSelectionOperationalScorecard
                {
                    Key = "off_hours_load",
                    Label = "Off-hours load indicator",
                    Status = CuratedOperationalScorecardStatus.Unavailable,
                    Summary = "Nedostatek bodů pro spolehlivé vyhodnocení off-hours loadu.",
                    Methodology = "Vyžadováno alespoň 4 body v aktivních i mimoaktivních hodinách."
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
                        CuratedOperationalScorecardStatus.Issue => "Víkendový load se blíží pracovnímu režimu.",
                        CuratedOperationalScorecardStatus.Watch => "Víkendový load je zvýšený oproti očekávání.",
                        _ => "Víkendový load je jasně oddělen od pracovních dní."
                    },
                    Methodology = $"Poměr průměrného weekend loadu ku průměrnému weekday loadu. Sample count weekday/weekend: {weekdayCount}/{weekendCount}."
                });
            }
            else
            {
                scorecards.Add(new CuratedSelectionOperationalScorecard
                {
                    Key = "weekend_load",
                    Label = "Weekend load indicator",
                    Status = CuratedOperationalScorecardStatus.Unavailable,
                    Summary = "Nedostatek weekday/weekend dat pro stabilní weekend indikátor.",
                    Methodology = "Vyžadováno alespoň 4 body ve weekday i weekend vzorku."
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
                        CuratedOperationalScorecardStatus.Issue => "Baseload tvoří dominantní část průměrného loadu.",
                        CuratedOperationalScorecardStatus.Watch => "Baseload je zvýšený a omezuje denní flexibilitu.",
                        _ => "Baseload intenzita je přiměřená vůči průměrnému loadu."
                    },
                    Methodology = "Poměr baseload proxy (20. percentil |kW|) ku avg(|kW|) z operating regime vrstvy."
                });
            }
            else
            {
                scorecards.Add(new CuratedSelectionOperationalScorecard
                {
                    Key = "baseload_intensity",
                    Label = "Baseload intensity indicator",
                    Status = CuratedOperationalScorecardStatus.Unavailable,
                    Summary = "Baseload intensity nelze vyhodnotit bez operating regime metrik.",
                    Methodology = "Využívá metriky baseload proxy a avg(|kW|)."
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
                Summary = "Selection je generation-only, load indikátor se nepoužije.",
                Methodology = "V1 load indikátory jsou určeny pro selection s nenulovou spotřebou."
            });
            scorecards.Add(new CuratedSelectionOperationalScorecard
            {
                Key = "weekend_load",
                Label = "Weekend load indicator",
                Status = CuratedOperationalScorecardStatus.Unavailable,
                Summary = "Selection je generation-only, weekend load indikátor se nepoužije.",
                Methodology = "V1 load indikátory jsou určeny pro selection s nenulovou spotřebou."
            });
            scorecards.Add(new CuratedSelectionOperationalScorecard
            {
                Key = "baseload_intensity",
                Label = "Baseload intensity indicator",
                Status = CuratedOperationalScorecardStatus.Unavailable,
                Summary = "Selection je generation-only, baseload intensity se nepoužije.",
                Methodology = "V1 baseload intensity je určena primárně pro load režim."
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
                    CuratedOperationalScorecardStatus.Issue => "Režim je výrazně špičkový a zatěžující.",
                    CuratedOperationalScorecardStatus.Watch => "Peak stress je zvýšený a může zvyšovat provozní náklady.",
                    _ => "Peak stress zůstává v běžném provozním pásmu."
                },
                Methodology = "Transparentně kombinuje peak significance ratio (peak analysis) a peak-to-average ratio (operating regime)."
            });
        }
        else
        {
            scorecards.Add(new CuratedSelectionOperationalScorecard
            {
                Key = "peak_stress",
                Label = "Peak stress indicator",
                Status = CuratedOperationalScorecardStatus.Unavailable,
                Summary = "Peak stress nelze vyhodnotit bez peak/regime metrik.",
                Methodology = "Vyžaduje dostupný peak significance nebo peak-to-average ratio."
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
                    CuratedOperationalScorecardStatus.Issue => "Selection má výraznou exportní tendenci a nízkou lokální využitelnost výroby.",
                    CuratedOperationalScorecardStatus.Watch => "Lokální využití výroby je omezené, export se objevuje častěji.",
                    _ => "Výroba je převážně využita lokálně v rámci selection setu."
                },
                Methodology = "Local utilization = 1 - export share, kde export share je odhadnut z netto záporné bilance vůči celkové výrobě v intervalu."
            });
        }
        else
        {
            scorecards.Add(new CuratedSelectionOperationalScorecard
            {
                Key = "local_generation_utilization",
                Label = "Local generation utilization indicator",
                Status = CuratedOperationalScorecardStatus.Unavailable,
                Summary = "Selection nemá významnou výrobu, generation utilization indikátor se nepoužije.",
                Methodology = "Aktivní pouze pro selection s nenulovou generation složkou."
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
                Summary = "Mimo aktivní hodiny zůstává load vyšší, než je žádoucí pro efektivní schedule.",
                Evidence = offHoursCard.MetricDisplay,
                Methodology = "Odvozeno přímo z Off-hours load indicatoru."
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
                Summary = "Víkendový režim se příliš podobá pracovnímu provozu.",
                Evidence = weekendCard.MetricDisplay,
                Methodology = "Odvozeno přímo z Weekend load indicatoru."
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
                Summary = "Profil má zvýšenou špičkovost vůči typickému loadu.",
                Evidence = peakCard.MetricDisplay,
                Methodology = "Odvozeno z peak significance a peak-to-average metriky."
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
                Summary = "Rozdíl mezi aktivními a neaktivními hodinami je slabý.",
                Evidence = (activeInactiveSeparation.Value * 100.0).ToString("N0") + " % separation",
                Methodology = "Separation = 1 - min(1, off-hours/active ratio) z aggregate loadu."
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
                Summary = "Výroba není dost využita lokálně a selection má exportní charakter.",
                Evidence = generationCard.MetricDisplay,
                Methodology = "Odvozeno z local generation utilization indikátoru a signed energy bilance."
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
                "elevated_off_hours_load" => "Výběr vykazuje zvýšený load mimo pracovní hodiny.",
                "elevated_weekend_load" => "Víkendový režim se blíží pracovnímu provozu.",
                "excessive_peak_stress" => "Výběr má výrazně špičkový profil.",
                "weak_active_inactive_separation" => "Oddělení aktivních a neaktivních hodin je slabé.",
                "export_tendency" => "Selection má výrazný exportní charakter.",
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
            opportunities.Add("V aktuálním intervalu se neukazuje výrazný schedule inefficiency kandidát.");
        }

        var issueCount = orderedInefficiencies.Count(item => item.Severity == CuratedSelectionInefficiencySeverity.Issue);
        var watchCount = orderedInefficiencies.Count(item => item.Severity == CuratedSelectionInefficiencySeverity.Watch);

        var summary = issueCount > 0
            ? $"EMS evaluation detekuje {issueCount} schedule inefficiency issue a {watchCount} watch signálů."
            : watchCount > 0
                ? $"EMS evaluation eviduje {watchCount} schedule inefficiency watch signálů bez tvrdého issue."
                : "EMS evaluation neukazuje výraznou schedule inefficiency v aktuálním intervalu.";

        var methodology = "EMS evaluation v1 kombinuje transparentní scorecards nad existujícími metrikami: off-hours/active ratio, weekend/weekday ratio, baseload/avg ratio, peak stress ratio a local generation utilization. Bez black-box skóre a bez optimalizačního enginu.";

        if (loadProfile.IsFallback)
        {
            methodology += " Pro krátké intervaly používá load profile fallback snapshot, takže weekend/off-hours signály mají nižší robustnost.";
        }

        return new CuratedSelectionEmsEvaluationSummary
        {
            IsAvailable = true,
            HasMixedSigns = hasMixedSigns,
            HasConsumption = hasConsumption,
            HasGeneration = hasGeneration,
            Summary = summary,
            Methodology = methodology,
            DistinctionNote = "Issue = datový/coverage problém, anomaly = deviation proti baseline, inefficiency = stabilní provozní neefektivita režimu.",
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

        var filePath = ResolveCuratedFilePath(source.FileName);
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

    public async Task<CuratedNodeCompareTimeSeriesResult?> GetCuratedCompareTimeSeriesAsync(
        string primaryNodeKey,
        IEnumerable<string> compareNodeKeys,
        DateTime from,
        DateTime to,
        CuratedNodeTimeSeriesMode mode = CuratedNodeTimeSeriesMode.Auto,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(primaryNodeKey) || !ComparePreviewSupportedNodeKeys.Contains(primaryNodeKey))
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

            if (!ComparePreviewSupportedNodeKeys.Contains(rawNodeKey))
            {
                excludedNodeMessages.Add($"{rawNodeKey}: compare preview pro tento uzel není v této verzi podporován.");
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
            excludedNodeMessages.Add($"{primaryNodeKey}: {primaryTimeSeries.NoDataMessage ?? "v analysis window nejsou dostupná data"}.");
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
                    excludedNodeMessages.Add($"{nodeKey}: nepodařilo se připravit časovou řadu.");
                    continue;
                }

                if (result.Points.Count == 0)
                {
                    excludedNodeMessages.Add($"{nodeKey}: {result.NoDataMessage ?? "v analysis window nejsou dostupná data"}.");
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
            ? "Compare preview nemá žádnou dostupnou výkonovou řadu pro vybrané uzly a interval."
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

        var filePath = ResolveCuratedFilePath(source.FileName);
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
        if (nodeKey == "weather_main")
        {
            return new CuratedNodeDeviationSummary
            {
                IsAvailable = false,
                Methodology = "Uzel počasí je v tomto sprintu jen vysvětlující faktor, deviation baseline se zde zatím nevyhodnocuje.",
                Message = "Baseline není pro tento uzel / interval zatím k dispozici.",
                Unit = "°C"
            };
        }

        var source = ResolveCuratedNodeSource(nodeKey);
        if (source is null || !source.SupportsDeviation)
        {
            return new CuratedNodeDeviationSummary
            {
                IsAvailable = false,
                Methodology = BaselineMethodology,
                Message = "Baseline není pro tento uzel / interval zatím k dispozici."
            };
        }

        var filePath = ResolveCuratedFilePath(source.FileName);
        if (filePath is null)
        {
            return new CuratedNodeDeviationSummary
            {
                IsAvailable = false,
                Methodology = BaselineMethodology,
                Message = "Chybí lokální reduced source."
            };
        }

        return await CalculateDeviationAsync(nodeKey, filePath, source, from, to, ct);
    }

    private sealed class CuratedNodeSource
    {
        public required string FileName { get; init; }
        public required string ColumnName { get; init; }
        public required string Title { get; init; }
        public string? NodeTypeHint { get; init; }
        public required string Unit { get; init; }
        public string SummaryLabel { get; init; } = "Souhrn";
        public string StatsUnit { get; init; } = string.Empty;
        public string StatsLabel { get; init; } = "Hodnota";
        public bool IsPowerSignal { get; init; }
        public double PowerToKilowattFactor { get; init; } = 1.0;
        public bool SupportsDeviation { get; init; } = true;
    }

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

    private sealed record TimeSeriesGranularityDecision(
        CuratedNodeTimeSeriesGranularity Granularity,
        string Label,
        string AggregationMethod,
        CuratedNodeTimeSeriesMode RequestedMode,
        string RequestedModeLabel);

    private CuratedNodeSource? ResolveCuratedNodeSource(string nodeKey)
    {
        return nodeKey switch
        {
            "pv_main" => new CuratedNodeSource
            {
                FileName = "electricity_P.csv",
                ColumnName = "PV",
                Title = "Solární výroba (PV)",
                NodeTypeHint = "generator_pv",
                Unit = "kWh",
                SummaryLabel = "Intervalová energie",
                StatsUnit = "kW",
                StatsLabel = "Výkon",
                IsPowerSignal = true,
                PowerToKilowattFactor = 0.001,
                SupportsDeviation = true
            },
            "chp_main" => new CuratedNodeSource
            {
                FileName = "electricity_P.csv",
                ColumnName = "CHP",
                Title = "Výroba kogenerace (CHP)",
                NodeTypeHint = "generator_chp",
                Unit = "kWh",
                SummaryLabel = "Intervalová energie",
                StatsUnit = "kW",
                StatsLabel = "Výkon",
                IsPowerSignal = true,
                PowerToKilowattFactor = 0.001,
                SupportsDeviation = true
            },
            "cooling_main" => new CuratedNodeSource
            {
                FileName = "cooling_P.csv",
                ColumnName = "total",
                Title = "Celkové chlazení",
                NodeTypeHint = "utility_cooling",
                Unit = "kWh",
                SummaryLabel = "Intervalová energie",
                StatsUnit = "kW",
                StatsLabel = "Výkon",
                IsPowerSignal = true,
                PowerToKilowattFactor = 0.001,
                SupportsDeviation = true
            },
            "heating_main" => new CuratedNodeSource
            {
                FileName = "heating_P.csv",
                ColumnName = "total",
                Title = "Celkové vytápění",
                NodeTypeHint = "utility_heating",
                Unit = "kWh",
                SummaryLabel = "Intervalová energie",
                StatsUnit = "kW",
                StatsLabel = "Výkon",
                IsPowerSignal = true,
                PowerToKilowattFactor = 0.001,
                SupportsDeviation = true
            },
            "weather_main" => new CuratedNodeSource
            {
                FileName = "weather.csv",
                ColumnName = "WeatherStation.Weather.Ta",
                Title = "Okamžitá průměrná teplota",
                NodeTypeHint = "weather",
                Unit = "°C",
                SummaryLabel = "Průměrná teplota",
                StatsUnit = "°C",
                StatsLabel = "Teplota",
                SupportsDeviation = false
            },
            _ => null
        };
    }

    private string? ResolveCuratedFilePath(string fileName)
    {
        var curatedPath = Path.Combine(_env.ContentRootPath, "..", "DataSet", "curated", fileName);
        if (File.Exists(curatedPath))
        {
            return curatedPath;
        }

        var dataPath = Path.Combine(_env.ContentRootPath, "..", "DataSet", "data", fileName);
        if (File.Exists(dataPath))
        {
            return dataPath;
        }

        var rootPath = Path.Combine(_env.ContentRootPath, "..", "DataSet", fileName);
        if (File.Exists(rootPath))
        {
            return rootPath;
        }

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
                Message = "Neplatný časový rozsah pro výpočet baseline."
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
                Message = "Baseline nelze připravit: chybí referenční baseline okna."
            };
        }

        try
        {
            using var reader = new StreamReader(filePath);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(headerLine))
            {
                return new CuratedNodeDeviationSummary
                {
                    IsAvailable = false,
                    Unit = source.Unit,
                    Methodology = BaselineMethodology,
                    Message = "Reduced source je prázdný."
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
                    Message = "Reduced source neobsahuje očekávaný sloupec pro vybraný uzel."
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
                    Message = "Pro vybraný interval nejsou k dispozici žádná data."
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
                    Message = "Baseline nelze spočítat: nejsou dostupná referenční baseline okna s dostatečným pokrytím dat."
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
                    Message = "Baseline je pro tento interval příliš nízká pro stabilní procentní vyhodnocení odchylky."
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
                Message = "Nepodařilo se načíst reduced source pro baseline výpočet."
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
                $"Stejné období minulého roku (-{yearOffset}y)"));
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
                $"Předchozí srovnatelné období (-{offsetIndex}x interval)"));
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
                "Stejné období v minulých letech (medián)",
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
                "Hybrid: stejné období minulý rok + recent comparable období",
                referenceCandidates
            );
        }

        if (samePeriodReferenceAggregates.Count == 1)
        {
            return new BaselineSelection(
                samePeriodReferenceAggregates[0].Sum,
                "Stejné období minulý rok",
                [samePeriodReferenceAggregates[0].Candidate]
            );
        }

        if (recentComparableReferenceAggregates.Count >= 2)
        {
            return new BaselineSelection(
                Median(recentComparableReferenceAggregates.Select(x => x.Sum).ToList()),
                "Předchozí srovnatelná období stejné délky (medián)",
                recentComparableReferenceAggregates.Select(x => x.Candidate).ToList()
            );
        }

        if (recentComparableReferenceAggregates.Count == 1)
        {
            return new BaselineSelection(
                recentComparableReferenceAggregates[0].Sum,
                "Bezprostředně předchozí srovnatelné období",
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

        // U velmi nízké baseline je procentní odchylka numericky nestabilní.
        return Math.Max(0.5, intervalDuration.TotalHours * 0.05);
    }

    private static string BuildMethodologyText(string strategyDescription, DateTime from, DateTime to, int minimumReferenceSamples)
    {
        return $"{BaselineMethodology} Strategy: {strategyDescription}. Analysis window: {from:u} - {to:u}. Minimum pokrytí referenčního okna: {minimumReferenceSamples} vzorků.";
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
            return CreateUnavailableWeatherExplanation("Vysvětlení počasím není dostupné, protože chybí weather.csv source.");
        }

        var weatherAverages = await GetWeatherAveragesAsync(weatherFilePath, analysisFrom, analysisTo, referenceCandidates, minimumReferenceSamples, ct);
        if (!weatherAverages.CurrentAverageTempC.HasValue)
        {
            return CreateUnavailableWeatherExplanation("Vysvětlení počasím není dostupné: v aktuálním intervalu chybí data venkovní teploty.");
        }

        if (weatherAverages.ReferenceAverageTempC.Count == 0)
        {
            return CreateUnavailableWeatherExplanation("Vysvětlení počasím není dostupné: v referenčním baseline období chybí dostatek weather dat.");
        }

        var referenceAverage = weatherAverages.ReferenceAverageTempC.Count >= 2
            ? Median(weatherAverages.ReferenceAverageTempC)
            : weatherAverages.ReferenceAverageTempC[0];
        var currentAverage = weatherAverages.CurrentAverageTempC.Value;
        var deltaTemp = currentAverage - referenceAverage;

        var status = WeatherExplanationStatus.NotSupportedByWeather;
        var conclusion = "Počasí odchylku v tomto intervalu nepodporuje.";

        if (Math.Abs(deltaTemp) < WeatherExplanationDeltaThresholdC)
        {
            return new WeatherExplanationSummary
            {
                IsAvailable = true,
                Status = WeatherExplanationStatus.WeatherChangeNeutral,
                CurrentAverageOutdoorTempC = currentAverage,
                ReferenceAverageOutdoorTempC = referenceAverage,
                DeltaOutdoorTempC = deltaTemp,
                Conclusion = "Rozdíl počasí vůči referenci je malý.",
                Methodology = "Explanatory heuristika v1: porovnání průměrné venkovní teploty v analysis window vůči referenčním baseline oknům. Při |delta T| < 0.8 °C je změna počasí považována za malou."
            };
        }

        if (nodeKey == "heating_main")
        {
            var isColderThanReference = deltaTemp <= -WeatherExplanationDeltaThresholdC;
            var isWarmerThanReference = deltaTemp >= WeatherExplanationDeltaThresholdC;

            if ((deltaAbsolute > 0 && isColderThanReference) || (deltaAbsolute < 0 && isWarmerThanReference))
            {
                status = WeatherExplanationStatus.SupportedByWeather;
                conclusion = "Odchylka může být částečně vysvětlena počasím.";
            }
        }
        else if (nodeKey == "cooling_main")
        {
            var isWarmerThanReference = deltaTemp >= WeatherExplanationDeltaThresholdC;
            var isColderThanReference = deltaTemp <= -WeatherExplanationDeltaThresholdC;

            if ((deltaAbsolute > 0 && isWarmerThanReference) || (deltaAbsolute < 0 && isColderThanReference))
            {
                status = WeatherExplanationStatus.SupportedByWeather;
                conclusion = "Odchylka může být částečně vysvětlena počasím.";
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
            Methodology = "Explanatory heuristika v1: porovnání průměrné venkovní teploty v analysis window vůči referenčním baseline oknům použitým baseline strategií."
        };
    }

    private static WeatherExplanationSummary CreateUnavailableWeatherExplanation(string message)
    {
        return new WeatherExplanationSummary
        {
            IsAvailable = false,
            Status = WeatherExplanationStatus.Unavailable,
            Conclusion = message,
            Methodology = "Explanatory heuristika vyžaduje dostupná weather data pro aktuální i referenční období."
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
            using var reader = new StreamReader(weatherFilePath);
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
            using var reader = new StreamReader(filePath);
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
            using var reader = new StreamReader(filePath);
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

    private static double NormalizeValueForAggregation(double rawValue, CuratedNodeSource source, double sampleStepHours)
    {
        if (!source.IsPowerSignal)
        {
            return rawValue;
        }

        var powerKw = NormalizeValueForStats(rawValue, source);
        return powerKw * sampleStepHours;
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
        var valueKind = source.IsPowerSignal ? "výkonu" : "hodnoty";

        if (requestedMode == CuratedNodeTimeSeriesMode.Raw15Min)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.Raw15Min,
                source.IsPowerSignal ? "Raw 15min výkon" : "Raw detailní řada",
                "Ruční režim 15min: bez agregace, zobrazeny jsou původní vzorky časové řady.",
                CuratedNodeTimeSeriesMode.Raw15Min,
                "15min"
            );
        }

        if (requestedMode == CuratedNodeTimeSeriesMode.HourlyAverage)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.HourlyAverage,
                source.IsPowerSignal ? "Hodinový průměr výkonu" : "Hodinový průměr hodnoty",
                $"Ruční režim Hourly: každá hodnota je aritmetický průměr {valueKind} v daném hodinovém bucketu.",
                CuratedNodeTimeSeriesMode.HourlyAverage,
                "Hourly"
            );
        }

        if (requestedMode == CuratedNodeTimeSeriesMode.DailyAverage)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.DailyAverage,
                source.IsPowerSignal ? "Denní průměr výkonu" : "Denní průměr hodnoty",
                $"Ruční režim Daily: každá hodnota je aritmetický průměr {valueKind} v daném denním bucketu.",
                CuratedNodeTimeSeriesMode.DailyAverage,
                "Daily"
            );
        }

        if (duration <= RawTimeSeriesThreshold)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.Raw15Min,
                source.IsPowerSignal ? "Raw 15min výkon" : "Raw detailní řada",
                "Auto režim: bez agregace, zobrazeny jsou původní vzorky časové řady.",
                CuratedNodeTimeSeriesMode.Auto,
                "Auto"
            );
        }

        if (duration <= HourlyTimeSeriesThreshold)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.HourlyAverage,
                source.IsPowerSignal ? "Hodinový průměr výkonu" : "Hodinový průměr hodnoty",
                $"Auto režim: agregace po hodinách, každá hodnota je aritmetický průměr {valueKind} v daném hodinovém bucketu.",
                CuratedNodeTimeSeriesMode.Auto,
                "Auto"
            );
        }

        return new TimeSeriesGranularityDecision(
            CuratedNodeTimeSeriesGranularity.DailyAverage,
            source.IsPowerSignal ? "Denní průměr výkonu" : "Denní průměr hodnoty",
            $"Auto režim: agregace po dnech, každá hodnota je aritmetický průměr {valueKind} v daném denním bucketu.",
            CuratedNodeTimeSeriesMode.Auto,
            "Auto"
        );
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
        if (source.IsPowerSignal)
        {
            return granularity.Granularity switch
            {
                CuratedNodeTimeSeriesGranularity.HourlyAverage => "Graf zobrazuje hodinově agregovaný průměr výkonu (kW). Souhrn nad grafem zůstává intervalová energie (kWh).",
                CuratedNodeTimeSeriesGranularity.DailyAverage => "Graf zobrazuje denně agregovaný průměr výkonu (kW). Souhrn nad grafem zůstává intervalová energie (kWh).",
                _ => "Graf zobrazuje okamžitý výkon v čase (kW) v původním kroku časové řady (~15 min). Souhrn nad grafem zobrazuje intervalovou energii (kWh)."
            };
        }

        return granularity.Granularity switch
        {
            CuratedNodeTimeSeriesGranularity.HourlyAverage => "Graf zobrazuje hodinově agregovaný průměr hodnoty pro vybraný uzel.",
            CuratedNodeTimeSeriesGranularity.DailyAverage => "Graf zobrazuje denně agregovaný průměr hodnoty pro vybraný uzel.",
            _ => "Graf zobrazuje okamžitou hodnotu v čase pro vybraný uzel."
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
            CuratedNodeTimeSeriesGranularity.HourlyAverage => "Compare preview zobrazuje výkonové řady v čase (kW) pro více uzlů. Hodinová agregace znamená průměr výkonu za hodinový bucket.",
            CuratedNodeTimeSeriesGranularity.DailyAverage => "Compare preview zobrazuje výkonové řady v čase (kW) pro více uzlů. Denní agregace znamená průměr výkonu za denní bucket.",
            _ => "Compare preview zobrazuje výkonové řady v čase (kW) pro více uzlů v původním detailním kroku dat (~15 min)."
        };
    }

    private static string ResolveSelectionAggregateInterpretationNote(CuratedNodeTimeSeriesGranularity granularity, CuratedAggregateEnergyProfile energyProfile)
    {
        var signedSemanticsNote = energyProfile switch
        {
            CuratedAggregateEnergyProfile.MixedSigned => " Selection kombinuje spotřebu i výrobu: kladné body reprezentují čistou spotřebu, záporné body čistou výrobu/export.",
            CuratedAggregateEnergyProfile.GenerationOnly => " Selection je výrobní/exportní: záporné body jsou očekávané a reprezentují export nebo výrobu.",
            _ => string.Empty
        };

        return granularity switch
        {
            CuratedNodeTimeSeriesGranularity.HourlyAverage => "Agregovaný graf zobrazuje součet hodinových průměrů výkonu (kW) přes podporované uzly selection setu." + signedSemanticsNote,
            CuratedNodeTimeSeriesGranularity.DailyAverage => "Agregovaný graf zobrazuje součet denních průměrů výkonu (kW) přes podporované uzly selection setu." + signedSemanticsNote,
            _ => "Agregovaný graf zobrazuje součet okamžitých výkonů (kW) přes podporované uzly selection setu v původním kroku dat (~15 min)." + signedSemanticsNote
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
            0 => "Disaggregation foundation: v intervalu nejsou dostupní measured contributoři.",
            1 => $"Aggregate je složen z 1 measured contributoru: {measuredContributors[0].Label}.",
            _ when hasMixedSigns => $"Aggregate je složen z {measuredContributors.Count} measured contributoru (load {consumptionContributorCount}, generation {generationContributorCount}).",
            _ when energyProfile == CuratedAggregateEnergyProfile.GenerationOnly => $"Aggregate je složen z {measuredContributors.Count} measured contributoru, kteri snizuji netto bilanci (generation/export).",
            _ => $"Aggregate je slozen z {measuredContributors.Count} measured contributoru, kteri zvysuji load (consumption)."
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
            return "Dominantni source nelze urcit: selection nema measured contributory s daty.";
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
                return $"Dominantni load slozka: {dominantLoad.Label}. Netto bilanci nejvice snizuje: {dominantGeneration.Label}.";
            }
        }

        return dominant.Direction switch
        {
            CuratedContributionDirection.IncreasesLoad => $"Dominantni slozkou vyberu je {dominant.Label}, ktera zvysuje load.",
            CuratedContributionDirection.ReducesNetBalance => $"Netto bilanci nejvice snizuje contributor {dominant.Label} (generation/export).",
            _ => $"Dominantni contributor je {dominant.Label}."
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
            ? "Source map je prazdna: selection je prazdny nebo bez analytickych contributoru."
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
                (netEnergyKwh, "Netto bilance energie", "Selection kombinuje spotřebu i výrobu; hlavní KPI proto reprezentuje netto výsledek.", true),
            CuratedAggregateEnergyProfile.GenerationOnly =>
                (totalGenerationKwh, "Celková výroba energie", "Selection je čistě výrobní/exportní; netto bilance je záporná.", false),
            CuratedAggregateEnergyProfile.ConsumptionOnly =>
                (totalConsumptionKwh, "Celková spotřeba energie", "Selection je čistě spotřební; netto bilance odpovídá spotřebě.", false),
            _ =>
                (netEnergyKwh, "Netto bilance energie", "Selection nemá v intervalu významné energetické contribution.", true)
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
            using var reader = new StreamReader(filePath);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(headerLine))
            {
                return CreateResult(
                    [],
                    "Reduced source je prázdný.",
                    [],
                    includeBaselineOverlay ? "Baseline overlay není dostupný: reduced source je prázdný." : null);
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
                    "Reduced source neobsahuje očekávaný sloupec pro vybraný uzel.",
                    [],
                    includeBaselineOverlay ? "Baseline overlay není dostupný: reduced source neobsahuje očekávaný sloupec." : null);
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

                if (timestamp < minRelevantFrom)
                {
                    continue;
                }

                if (timestamp >= maxRelevantTo)
                {
                    break;
                }

                if (!double.TryParse(cols[colIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var rawValue))
                {
                    continue;
                }

                var statsValue = NormalizeValueForStats(rawValue, source);
                var aggregationValue = NormalizeValueForAggregation(rawValue, source, sampleStepHours);

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
                        Value = x.Value.Sum / x.Value.Count
                    })
                    .ToList();

            IReadOnlyList<CuratedNodeTimeSeriesPoint> baselinePoints = [];
            string? baselineOverlayMessage = null;

            if (includeBaselineOverlay)
            {
                if (!source.SupportsDeviation)
                {
                    baselineOverlayMessage = "Baseline overlay není pro tento uzel podporován.";
                }
                else if (baselineCandidates.Count == 0)
                {
                    baselineOverlayMessage = "Baseline overlay není dostupný: chybí referenční baseline okna.";
                }
                else if (currentIntervalSamples == 0)
                {
                    baselineOverlayMessage = "Baseline overlay nelze spočítat, protože v analysis window chybí aktuální vzorky.";
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
                        baselineOverlayMessage = "Baseline overlay není dostupný: referenční baseline okna nemají dostatečné pokrytí dat.";
                    }
                    else
                    {
                        baselinePoints = BuildBaselineOverlaySeries(selectedBaseline, baselineBucketStatsByCandidate);
                        if (baselinePoints.Count == 0)
                        {
                            baselineOverlayMessage = "Baseline overlay není dostupný: referenční řada pro vybranou granularitu nemá žádné body.";
                        }
                    }
                }
            }

            return CreateResult(
                points,
                points.Count == 0 ? "Pro vybraný interval nejsou k dispozici žádné body časové řady." : null,
                baselinePoints,
                baselineOverlayMessage);
        }
        catch
        {
            return CreateResult(
                [],
                "Nepodařilo se načíst reduced source pro časovou řadu.",
                [],
                includeBaselineOverlay ? "Baseline overlay není dostupný: nepodařilo se načíst reduced source." : null);
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
            using var reader = new StreamReader(filePath);
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

                        if (timestamp >= from && timestamp < to)
                        {
                            if (double.TryParse(cols[colIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                            {
                                var statsValue = NormalizeValueForStats(val, source);
                                var aggregateValue = NormalizeValueForAggregation(val, source, sampleStepHours);

                                aggregateSum += aggregateValue;
                                statsSum += statsValue;
                                count++;
                                if (statsValue < min) min = statsValue;
                                if (statsValue > max) max = statsValue;
                            }
                        }
                        
                        // Zastaví čtení pokud jsme chronologicky přesáhli zkoumané období (zlepšuje výkon pro starší data)
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
            return null; // Při file I/O problému prostě fallbackujeme do NO-DATA stavu
        }
    }
}
