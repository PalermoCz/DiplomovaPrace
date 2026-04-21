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
    public string AggregationMethod { get; init; } = "Bez agregace (raw Ĺ™ada).";
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
    public string AggregationMethod { get; init; } = "Bez agregace (raw Ĺ™ada).";
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
    private const string BaselineMethodology = "Analysis window je vĹľdy pĹ™Ă­mo vybranĂ˝ interval uĹľivatele. Baseline strategy je urÄŤena separĂˇtnÄ›: priorita je stejnĂ© obdobĂ­ v minulĂ˝ch letech, fallback jsou pĹ™edchozĂ­ srovnatelnĂˇ obdobĂ­ stejnĂ© dĂ©lky. U Ĺ™ad s pĹ™Ă­ponou P se pĹ™ed souÄŤtem pĹ™evĂˇdĂ­ vĂ˝kon na energii podle kroku ÄŤasovĂ© Ĺ™ady (inferovanĂ˝ krok, fallback 15 min).";
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
    private readonly FacilityDataBindingRegistry _bindingRegistry;
    private readonly ConcurrentDictionary<string, DateTime> _maxTimestampCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, (DateTime MinUtc, DateTime MaxUtc)> _timeDomainCache = new(StringComparer.OrdinalIgnoreCase);

    public NodeAnalyticsPreviewService(IKpiService kpiService, IWebHostEnvironment env, FacilityDataBindingRegistry bindingRegistry)
    {
        _kpiService = kpiService;
        _env = env;
        _bindingRegistry = bindingRegistry;
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
    {
        var source = ResolveCuratedNodeSource(nodeKey);
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
                        ? "Baseline overlay nenĂ­ dostupnĂ˝: chybĂ­ lokĂˇlnĂ­ reduced source."
                        : "Baseline overlay nenĂ­ pro tento uzel podporovĂˇn.")
                    : null,
                NoDataMessage = "ChybĂ­ lokĂˇlnĂ­ reduced source pro vykreslenĂ­ ÄŤasovĂ© Ĺ™ady."
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
                    Summary = "Load profile nenĂ­ dostupnĂ˝, protoĹľe selection set neobsahuje podporovanĂ© energetickĂ© uzly.",
                    Methodology = "Daily profile v1 pouĹľĂ­vĂˇ transparentnĂ­ agregaci aggregate vĂ˝konovĂ© Ĺ™ady Selection Setu (hour-of-day average nebo fallback interval snapshot).",
                    DifferenceFromForecast = "Forecast je predikce budoucĂ­ho prĹŻbÄ›hu, load profile je popis typickĂ©ho historickĂ©ho chovĂˇnĂ­.",
                    DifferenceFromMainChart = "HlavnĂ­ chart zobrazuje prĹŻbÄ›h konkrĂ©tnĂ­ho intervalu; load profile agreguje opakujĂ­cĂ­ se pattern napĹ™Ă­ÄŤ intervalem."
                },
                PeakAnalysis = new CuratedSelectionPeakAnalysisSummary
                {
                    IsAvailable = false,
                    Summary = "Peak analysis nenĂ­ dostupnĂˇ bez podporovanĂ˝ch uzlĹŻ.",
                    Methodology = "Peak analysis v1 vyhodnocuje peak demand, peak generation/export a peak net absolute event z aggregate vĂ˝konovĂ© Ĺ™ady."
                },
                OperatingRegime = new CuratedSelectionOperatingRegimeSummary
                {
                    IsAvailable = false,
                    Summary = "Operating regime summary nenĂ­ dostupnĂˇ bez aggregate ÄŤasovĂ© Ĺ™ady.",
                    Methodology = "Operating regime v1 pouĹľĂ­vĂˇ transparentnĂ­ heuristiky nad aggregate vĂ˝konem: baseload proxy, peak-to-average, variabilitu a weekday/weekend signal."
                },
                EmsEvaluation = new CuratedSelectionEmsEvaluationSummary
                {
                    IsAvailable = false,
                    Summary = "EMS evaluation nenĂ­ dostupnĂˇ: selection set neobsahuje podporovanĂ© energetickĂ© uzly.",
                    Methodology = "EMS evaluation v1 stavĂ­ transparentnĂ­ scorecards pouze nad aggregate load profile, peak a operating regime metrikami.",
                    DistinctionNote = "Issue = datovĂ˝/coverage problĂ©m, anomaly = odchylka proti baseline, inefficiency = provoznĂ­ neefektivita reĹľimu."
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
                    Summary = "Forecast nenĂ­ dostupnĂ˝, protoĹľe selection set neobsahuje podporovanĂ© energetickĂ© uzly.",
                    ForecastPrinciple = "Comparable windows v1 (transparentnĂ­)",
                    Methodology = "Forecast pouĹľĂ­vĂˇ jen historickĂˇ referenÄŤnĂ­ okna pĹ™ed target intervalem; unsupported uzly se do vĂ˝poÄŤtu nezahrnujĂ­.",
                    MetricsNote = "DiagnostickĂ© metriky MAE/RMSE/Bias/WAPE se poÄŤĂ­tajĂ­ pouze pĹ™i dostupnĂ© forecast i actual sĂ©rii.",
                    SupportedNodeCount = 0,
                    IncludedNodeCount = 0,
                    ForecastProviderNodeCount = 0,
                    ForecastMissingNodeCount = 0,
                    UsesTargetLeakage = false,
                    Signals = ["no_supported_nodes"]
                },
                Message = "Selection set neobsahuje kompatibilnĂ­ energetickĂ© uzly pro agregaci overview."
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
                StatsLabel = "AgregovanĂ˝ vĂ˝kon"
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
                    baselineOverlayMessage = "Baseline overlay nenĂ­ v aggregate reĹľimu dostupnĂ˝ pro ĹľĂˇdnĂ˝ podporovanĂ˝ uzel.";
                }
                else if (baselineProviders < timeSeries.Count)
                {
                    baselineOverlayMessage = $"Baseline overlay je agregovĂˇn z {baselineProviders}/{timeSeries.Count} podporovanĂ˝ch uzlĹŻ.";
                }
            }

            aggregateTimeSeries = new CuratedNodeTimeSeriesResult
            {
                NodeKey = "selection_set",
                Title = supportedNodeKeys.Count == 1 ? template.Title : "Aggregate vĂ˝kon Selection Setu",
                Unit = template.Unit,
                YAxisLabel = template.YAxisLabel,
                Granularity = template.Granularity,
                GranularityLabel = template.GranularityLabel,
                AggregationMethod = $"{template.AggregationMethod} Selection aggregate: suma vĂ˝konĹŻ podporovanĂ˝ch uzlĹŻ v kaĹľdĂ©m ÄŤasovĂ©m bodÄ›.",
                InterpretationNote = ResolveSelectionAggregateInterpretationNote(template.Granularity, energyProfile),
                RequestedMode = template.RequestedMode,
                RequestedModeLabel = template.RequestedModeLabel,
                BaselineOverlayRequested = includeBaselineOverlay,
                BaselineOverlayAvailable = baselinePoints.Count > 0,
                BaselineOverlayMessage = baselineOverlayMessage,
                BaselinePoints = baselinePoints,
                NoDataMessage = points.Count == 0
                    ? "Pro podporovanĂ© uzly selection setu nejsou v intervalu dostupnĂ© body ÄŤasovĂ© Ĺ™ady."
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
            messageParts.Add($"IgnorovĂˇno nepodporovanĂ˝ch uzlĹŻ: {unsupportedNodeKeys.Count}");
        }

        if (contextOnlyNodeKeys.Count > 0)
        {
            messageParts.Add($"Context-only/excluded uzly: {contextOnlyNodeKeys.Count}");
        }

        if (noDataNodeKeys.Count > 0)
        {
            messageParts.Add($"UzlĹŻ bez dat v intervalu: {noDataNodeKeys.Count}");
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
            ForecastPrinciple = "Comparable windows v1 (transparentnĂ­)",
            Methodology = "Forecast je konstruovĂˇn z historickĂ˝ch referenÄŤnĂ­ch oken (stejnĂ© obdobĂ­ v minulĂ˝ch letech + recent comparable intervaly). Do forecastu vstupujĂ­ pouze data dostupnĂˇ pĹ™ed target intervalem; leakage z target okna nenĂ­ pouĹľit.",
            MetricsNote = "MAE, RMSE a Bias jsou poÄŤĂ­tĂˇny nad podepsanĂ˝m vĂ˝konem (kW). WAPE pouĹľĂ­vĂˇ sumu absolutnĂ­ch actual hodnot, takĹľe je robustnĂ­ i pro mixed-sign selection.",
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
                Summary = "Forecast vs actual nelze vyhodnotit, protoĹľe aggregate actual Ĺ™ada nemĂˇ body.",
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
                Summary = "Forecast reference nenĂ­ pro aktuĂˇlnĂ­ interval dostupnĂˇ (nedostateÄŤnĂ© historical windows).",
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
                Summary = "Actual a forecast Ĺ™ada nemajĂ­ ÄŤasovĂ˝ pĹ™ekryv po agregaci.",
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
            CuratedSelectionForecastStatus.Stable => "Forecast sedĂ­ stabilnÄ›, rozdĂ­l actual vs forecast je nĂ­zkĂ˝.",
            CuratedSelectionForecastStatus.Watch => "Forecast je pouĹľitelnĂ˝, ale odchylka actual vs forecast vyĹľaduje pozornost.",
            CuratedSelectionForecastStatus.PoorFit => "Forecast mĂˇ slabou shodu s actual Ĺ™adou v aktuĂˇlnĂ­m intervalu.",
            CuratedSelectionForecastStatus.LimitedData => "Forecast je jen orientaÄŤnĂ­: nĂ­zkĂ˝ pĹ™ekryv nebo mĂˇlo aligned bodĹŻ.",
            _ => "Forecast nenĂ­ dostupnĂ˝."
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
            compareExcludedMessages.Add($"Forecast reference nebyla dostupnĂˇ pro {baseDiagnostics.ForecastMissingNodeCount}/{Math.Max(1, baseDiagnostics.IncludedNodeCount)} analyticky zahrnutĂ˝ch uzlĹŻ.");
        }

        var compareData = new CuratedNodeCompareTimeSeriesResult
        {
            PrimaryNodeKey = "selection_set",
            Title = "Forecast vs Actual",
            Unit = aggregateActualTimeSeries.Unit,
            YAxisLabel = aggregateActualTimeSeries.YAxisLabel,
            Granularity = aggregateActualTimeSeries.Granularity,
            GranularityLabel = aggregateActualTimeSeries.GranularityLabel,
            AggregationMethod = "Forecast v1: transparent comparable windows (historickĂˇ okna pĹ™ed target intervalem). Actual i forecast jsou agregovanĂ© pĹ™es Selection Set bez unsupported uzlĹŻ.",
            InterpretationNote = "Forecast je predikce oÄŤekĂˇvanĂ©ho prĹŻbÄ›hu, zatĂ­mco baseline/deviation vrstva zĹŻstĂˇvĂˇ samostatnĂ˝ referenÄŤnĂ­ mechanismus pro alerting.",
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
            CuratedSelectionAnomalyStatus.DataIssue when hasNoSupportedNodes => "Selection je mimo podporovanĂ© analytickĂ© uzly.",
            CuratedSelectionAnomalyStatus.DataIssue when hasNoIncludedNodes => "PodporovanĂ© uzly nemajĂ­ v intervalu pouĹľitelnĂˇ data.",
            CuratedSelectionAnomalyStatus.DataIssue => "DatovĂˇ kvalita je nĂ­zkĂˇ, analytickĂ˝ vĂ˝stup mĂˇ omezenou dĹŻvÄ›ryhodnost.",
            CuratedSelectionAnomalyStatus.Suspicious when highDeviationCount > 0 => "DetekovĂˇna silnĂˇ deviation v ÄŤĂˇsti selection setu.",
            CuratedSelectionAnomalyStatus.Suspicious when hasAbruptShift => "AgregovanĂˇ Ĺ™ada obsahuje nĂˇhlĂ˝ skok oproti bÄ›ĹľnĂ© Ăşrovni.",
            CuratedSelectionAnomalyStatus.Suspicious => "Kombinace signĂˇlĹŻ indikuje podezĹ™elĂ© chovĂˇnĂ­.",
            CuratedSelectionAnomalyStatus.Attention when elevatedDeviationCount > 0 => "VĂ˝bÄ›r vykazuje zvĂ˝ĹˇenĂ© deviation signĂˇly.",
            CuratedSelectionAnomalyStatus.Attention when hasWeakCoverage => "Coverage je oslabenĂ©, interpretujte vĂ˝sledek opatrnÄ›.",
            CuratedSelectionAnomalyStatus.Attention when hasMixedSignedSelection => "Selection kombinuje spotĹ™ebu i vĂ˝robu, netto vyĹľaduje opatrnou interpretaci.",
            CuratedSelectionAnomalyStatus.Attention => "VĂ˝bÄ›r vyĹľaduje zvĂ˝Ĺˇenou pozornost.",
            _ => "Bez zjevnĂ© anomĂˇlie, deviation i coverage jsou stabilnĂ­."
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
                Summary = "Load profile nenĂ­ dostupnĂ˝: aggregate ÄŤasovĂˇ Ĺ™ada nemĂˇ dost bodĹŻ.",
                Methodology = "Daily profile v1 pouĹľĂ­vĂˇ average by hour-of-day nad aggregate vĂ˝konovou Ĺ™adou Selection Setu; pĹ™i krĂˇtkĂ©m intervalu pĹ™echĂˇzĂ­ na transparentnĂ­ interval snapshot.",
                DifferenceFromForecast = "Forecast je predikce oÄŤekĂˇvanĂ©ho budoucĂ­ho prĹŻbÄ›hu, load profile popisuje typickĂ˝ historickĂ˝ pattern.",
                DifferenceFromMainChart = "HlavnĂ­ chart ukazuje konkrĂ©tnĂ­ prĹŻbÄ›h v ÄŤase, load profile ukazuje opakujĂ­cĂ­ se tvar dne v agregovanĂ©m pohledu."
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
                    Summary = "Load profile nenĂ­ dostupnĂ˝: po hourly bucketizaci nezbyly ĹľĂˇdnĂ© validnĂ­ body.",
                    Methodology = "Daily profile v1 pouĹľĂ­vĂˇ average by hour-of-day nad aggregate vĂ˝konovou Ĺ™adou Selection Setu; pĹ™i krĂˇtkĂ©m intervalu pĹ™echĂˇzĂ­ na transparentnĂ­ interval snapshot.",
                    DifferenceFromForecast = "Forecast je predikce oÄŤekĂˇvanĂ©ho budoucĂ­ho prĹŻbÄ›hu, load profile popisuje typickĂ˝ historickĂ˝ pattern.",
                    DifferenceFromMainChart = "HlavnĂ­ chart ukazuje konkrĂ©tnĂ­ prĹŻbÄ›h v ÄŤase, load profile ukazuje opakujĂ­cĂ­ se tvar dne v agregovanĂ©m pohledu."
                };
            }

            var peakBucket = hourlyBuckets
                .OrderByDescending(bucket => Math.Abs(bucket.AverageKw))
                .First();

            var summary = hasMixedSigns
                ? $"Daily profile (hour-of-day) z {distinctDayCount} dnĂ­ a {orderedPoints.Count} bodĹŻ ({selectionScope}). Mixed-sign semantika je zachovĂˇna (+ load, - generation/export)."
                : $"Daily profile (hour-of-day) z {distinctDayCount} dnĂ­ a {orderedPoints.Count} bodĹŻ ({selectionScope}). NejvĂ˝raznÄ›jĹˇĂ­ hodinovĂ˝ bucket: {peakBucket.Label}.";

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
                Methodology = "Profil je transparentnĂ­ prĹŻmÄ›r aggregate vĂ˝konu (kW) podle hodiny dne nad zvolenĂ˝m intervalem. Bez black-box modelu.",
                DifferenceFromForecast = "Forecast vrstva odhaduje budoucĂ­ prĹŻbÄ›h; load profile vrstva shrnuje typickĂ© opakujĂ­cĂ­ se chovĂˇnĂ­ v rĂˇmci dne.",
                DifferenceFromMainChart = "HlavnĂ­ chart drĹľĂ­ chronologii konkrĂ©tnĂ­ch bodĹŻ intervalu; profile ztrĂˇcĂ­ konkrĂ©tnĂ­ datum a zachovĂˇvĂˇ jen typickĂ˝ intradennĂ­ pattern.",
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
            Summary = $"Interval je krĂˇtkĂ˝ ({distinctDayCount} den/dny), proto je pouĹľit snapshot po {fallbackBucketCount} segmentech mĂ­sto typickĂ©ho dennĂ­ho profilu.",
            Methodology = "Fallback profil pouĹľĂ­vĂˇ prĹŻmÄ›r aggregate vĂ˝konu (kW) v rovnomÄ›rnĂ˝ch segmentech aktuĂˇlnĂ­ho intervalu; nevydĂˇvĂˇ se za typickĂ˝ dennĂ­ profil.",
            DifferenceFromForecast = "Forecast vrstva predikuje dalĹˇĂ­ vĂ˝voj, fallback profile je pouze transparentnĂ­ strukturace aktuĂˇlnĂ­ho krĂˇtkĂ©ho intervalu.",
            DifferenceFromMainChart = "HlavnĂ­ chart ukazuje kaĹľdĂ˝ bod; fallback profile zhuĹˇĹĄuje interval do nÄ›kolika segmentĹŻ pro rychlĂ© ÄŤtenĂ­ provoznĂ­ho tvaru.",
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
                Summary = "Peak analysis nenĂ­ dostupnĂˇ bez aggregate ÄŤasovĂ© Ĺ™ady.",
                Methodology = "Peak analysis v1 vyhodnocuje peak demand, peak generation/export a peak net absolute event z aggregate vĂ˝konovĂ© Ĺ™ady Selection Setu."
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
            CuratedAggregateEnergyProfile.ConsumptionOnly => "Selection je consumption-only: hlavnĂ­ event je demand peak a jeho vĂ˝znamnost vĹŻÄŤi typickĂ©mu loadu.",
            CuratedAggregateEnergyProfile.GenerationOnly => "Selection je generation-only: hlavnĂ­ event je generation/export peak a jeho vĂ˝znamnost.",
            CuratedAggregateEnergyProfile.MixedSigned => "Selection je mixed-sign: sledujĂ­ se oddÄ›lenÄ› demand peak, generation peak i net absolute peak event.",
            _ => "Peak analysis je pouze orientaÄŤnĂ­, protoĹľe selection nemĂˇ vĂ˝raznĂ© energetickĂ© body."
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
            Methodology = "Peak analysis nenĂ­ jen KPI min/max: explicitnÄ› mapuje peak eventy na konkrĂ©tnĂ­ timestampy a porovnĂˇvĂˇ jejich magnitudu s typickou |kW| ĂşrovnĂ­ aggregate Ĺ™ady (mediĂˇn |kW|)."
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
                Summary = "Operating regime summary nenĂ­ dostupnĂˇ: aggregate Ĺ™ada je pĹ™Ă­liĹˇ krĂˇtkĂˇ.",
                Methodology = "Operating regime v1 pouĹľĂ­vĂˇ transparentnĂ­ heuristiky nad aggregate vĂ˝konem: baseload proxy, peak-to-average, variabilitu a weekday/weekend signal."
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
            ? "VĂ˝bÄ›r mĂˇ vĂ˝raznÄ› ĹˇpiÄŤkovĂ˝ profil."
            : (baseload / Math.Max(0.25, averageAbs)) >= 0.75 && variability < 0.45
                ? "VĂ˝bÄ›r mĂˇ stabilnĂ­ baseload charakter."
                : variability >= 0.60
                    ? "VĂ˝bÄ›r mĂˇ vĂ˝raznou dennĂ­ variabilitu."
                    : "VĂ˝bÄ›r mĂˇ vyvĂˇĹľenĂ˝ provoznĂ­ reĹľim bez extrĂ©mnĂ­ ĹˇpiÄŤkovosti.";

        if (weekdayWeekendDeltaPercent.HasValue && Math.Abs(weekdayWeekendDeltaPercent.Value) >= 20.0)
        {
            var direction = weekdayWeekendDeltaPercent.Value > 0 ? "weekday" : "weekend";
            summary += $" VĂ˝raznĂ˝ weekday/weekend rozdĂ­l ({direction} dominantnĂ­).";
        }

        if (energyProfile == CuratedAggregateEnergyProfile.MixedSigned)
        {
            summary += " Selection je mixed-sign, metriky jsou proto poÄŤĂ­tĂˇny nad |kW|.";
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
            Methodology = "Regime heuristika v1: baseload proxy = 20. percentil |kW|, peak-to-average = max(|kW|)/avg(|kW|), variability = std(|kW|)/avg(|kW|), weekday/weekend signal = rozdĂ­l prĹŻmÄ›rĹŻ normalizovanĂ˝ avg(|kW|).",
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
                Summary = "EMS evaluation v1 nenĂ­ dostupnĂˇ: aggregate Ĺ™ada je pĹ™Ă­liĹˇ krĂˇtkĂˇ nebo chybĂ­ operating regime metriky.",
                Methodology = "EMS evaluation v1 pouĹľĂ­vĂˇ transparentnĂ­ pravidla nad load profile, peak analysis a operating regime vrstvou.",
                DistinctionNote = "Issue = datovĂ˝/coverage problĂ©m, anomaly = odchylka proti baseline, inefficiency = provoznĂ­ neefektivita reĹľimu."
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
                        CuratedOperationalScorecardStatus.Issue => "Mimo aktivnĂ­ hodiny pĹ™etrvĂˇvĂˇ vysokĂ˝ load.",
                        CuratedOperationalScorecardStatus.Watch => "Mimo aktivnĂ­ hodiny je load zvĂ˝ĹˇenĂ˝.",
                        _ => "Mimo aktivnĂ­ hodiny load vĂ˝raznÄ› klesĂˇ."
                    },
                    Methodology = $"PrĹŻmÄ›r aggregate loadu mimo 07:00-19:00 vĹŻÄŤi prĹŻmÄ›ru v aktivnĂ­ch hodinĂˇch. Sample count active/off: {activeCount}/{offCount}."
                });
            }
            else
            {
                scorecards.Add(new CuratedSelectionOperationalScorecard
                {
                    Key = "off_hours_load",
                    Label = "Off-hours load indicator",
                    Status = CuratedOperationalScorecardStatus.Unavailable,
                    Summary = "Nedostatek bodĹŻ pro spolehlivĂ© vyhodnocenĂ­ off-hours loadu.",
                    Methodology = "VyĹľadovĂˇno alespoĹ 4 body v aktivnĂ­ch i mimoaktivnĂ­ch hodinĂˇch."
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
                        CuratedOperationalScorecardStatus.Issue => "VĂ­kendovĂ˝ load se blĂ­ĹľĂ­ pracovnĂ­mu reĹľimu.",
                        CuratedOperationalScorecardStatus.Watch => "VĂ­kendovĂ˝ load je zvĂ˝ĹˇenĂ˝ oproti oÄŤekĂˇvĂˇnĂ­.",
                        _ => "VĂ­kendovĂ˝ load je jasnÄ› oddÄ›len od pracovnĂ­ch dnĂ­."
                    },
                    Methodology = $"PomÄ›r prĹŻmÄ›rnĂ©ho weekend loadu ku prĹŻmÄ›rnĂ©mu weekday loadu. Sample count weekday/weekend: {weekdayCount}/{weekendCount}."
                });
            }
            else
            {
                scorecards.Add(new CuratedSelectionOperationalScorecard
                {
                    Key = "weekend_load",
                    Label = "Weekend load indicator",
                    Status = CuratedOperationalScorecardStatus.Unavailable,
                    Summary = "Nedostatek weekday/weekend dat pro stabilnĂ­ weekend indikĂˇtor.",
                    Methodology = "VyĹľadovĂˇno alespoĹ 4 body ve weekday i weekend vzorku."
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
                        CuratedOperationalScorecardStatus.Issue => "Baseload tvoĹ™Ă­ dominantnĂ­ ÄŤĂˇst prĹŻmÄ›rnĂ©ho loadu.",
                        CuratedOperationalScorecardStatus.Watch => "Baseload je zvĂ˝ĹˇenĂ˝ a omezuje dennĂ­ flexibilitu.",
                        _ => "Baseload intenzita je pĹ™imÄ›Ĺ™enĂˇ vĹŻÄŤi prĹŻmÄ›rnĂ©mu loadu."
                    },
                    Methodology = "PomÄ›r baseload proxy (20. percentil |kW|) ku avg(|kW|) z operating regime vrstvy."
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
                    Methodology = "VyuĹľĂ­vĂˇ metriky baseload proxy a avg(|kW|)."
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
                Summary = "Selection je generation-only, load indikĂˇtor se nepouĹľije.",
                Methodology = "V1 load indikĂˇtory jsou urÄŤeny pro selection s nenulovou spotĹ™ebou."
            });
            scorecards.Add(new CuratedSelectionOperationalScorecard
            {
                Key = "weekend_load",
                Label = "Weekend load indicator",
                Status = CuratedOperationalScorecardStatus.Unavailable,
                Summary = "Selection je generation-only, weekend load indikĂˇtor se nepouĹľije.",
                Methodology = "V1 load indikĂˇtory jsou urÄŤeny pro selection s nenulovou spotĹ™ebou."
            });
            scorecards.Add(new CuratedSelectionOperationalScorecard
            {
                Key = "baseload_intensity",
                Label = "Baseload intensity indicator",
                Status = CuratedOperationalScorecardStatus.Unavailable,
                Summary = "Selection je generation-only, baseload intensity se nepouĹľije.",
                Methodology = "V1 baseload intensity je urÄŤena primĂˇrnÄ› pro load reĹľim."
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
                    CuratedOperationalScorecardStatus.Issue => "ReĹľim je vĂ˝raznÄ› ĹˇpiÄŤkovĂ˝ a zatÄ›ĹľujĂ­cĂ­.",
                    CuratedOperationalScorecardStatus.Watch => "Peak stress je zvĂ˝ĹˇenĂ˝ a mĹŻĹľe zvyĹˇovat provoznĂ­ nĂˇklady.",
                    _ => "Peak stress zĹŻstĂˇvĂˇ v bÄ›ĹľnĂ©m provoznĂ­m pĂˇsmu."
                },
                Methodology = "TransparentnÄ› kombinuje peak significance ratio (peak analysis) a peak-to-average ratio (operating regime)."
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
                Methodology = "VyĹľaduje dostupnĂ˝ peak significance nebo peak-to-average ratio."
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
                    CuratedOperationalScorecardStatus.Issue => "Selection mĂˇ vĂ˝raznou exportnĂ­ tendenci a nĂ­zkou lokĂˇlnĂ­ vyuĹľitelnost vĂ˝roby.",
                    CuratedOperationalScorecardStatus.Watch => "LokĂˇlnĂ­ vyuĹľitĂ­ vĂ˝roby je omezenĂ©, export se objevuje ÄŤastÄ›ji.",
                    _ => "VĂ˝roba je pĹ™evĂˇĹľnÄ› vyuĹľita lokĂˇlnÄ› v rĂˇmci selection setu."
                },
                Methodology = "Local utilization = 1 - export share, kde export share je odhadnut z netto zĂˇpornĂ© bilance vĹŻÄŤi celkovĂ© vĂ˝robÄ› v intervalu."
            });
        }
        else
        {
            scorecards.Add(new CuratedSelectionOperationalScorecard
            {
                Key = "local_generation_utilization",
                Label = "Local generation utilization indicator",
                Status = CuratedOperationalScorecardStatus.Unavailable,
                Summary = "Selection nemĂˇ vĂ˝znamnou vĂ˝robu, generation utilization indikĂˇtor se nepouĹľije.",
                Methodology = "AktivnĂ­ pouze pro selection s nenulovou generation sloĹľkou."
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
                Summary = "Mimo aktivnĂ­ hodiny zĹŻstĂˇvĂˇ load vyĹˇĹˇĂ­, neĹľ je ĹľĂˇdoucĂ­ pro efektivnĂ­ schedule.",
                Evidence = offHoursCard.MetricDisplay,
                Methodology = "Odvozeno pĹ™Ă­mo z Off-hours load indicatoru."
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
                Summary = "VĂ­kendovĂ˝ reĹľim se pĹ™Ă­liĹˇ podobĂˇ pracovnĂ­mu provozu.",
                Evidence = weekendCard.MetricDisplay,
                Methodology = "Odvozeno pĹ™Ă­mo z Weekend load indicatoru."
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
                Summary = "Profil mĂˇ zvĂ˝Ĺˇenou ĹˇpiÄŤkovost vĹŻÄŤi typickĂ©mu loadu.",
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
                Summary = "RozdĂ­l mezi aktivnĂ­mi a neaktivnĂ­mi hodinami je slabĂ˝.",
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
                Summary = "VĂ˝roba nenĂ­ dost vyuĹľita lokĂˇlnÄ› a selection mĂˇ exportnĂ­ charakter.",
                Evidence = generationCard.MetricDisplay,
                Methodology = "Odvozeno z local generation utilization indikĂˇtoru a signed energy bilance."
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
                "elevated_off_hours_load" => "VĂ˝bÄ›r vykazuje zvĂ˝ĹˇenĂ˝ load mimo pracovnĂ­ hodiny.",
                "elevated_weekend_load" => "VĂ­kendovĂ˝ reĹľim se blĂ­ĹľĂ­ pracovnĂ­mu provozu.",
                "excessive_peak_stress" => "VĂ˝bÄ›r mĂˇ vĂ˝raznÄ› ĹˇpiÄŤkovĂ˝ profil.",
                "weak_active_inactive_separation" => "OddÄ›lenĂ­ aktivnĂ­ch a neaktivnĂ­ch hodin je slabĂ©.",
                "export_tendency" => "Selection mĂˇ vĂ˝raznĂ˝ exportnĂ­ charakter.",
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
            opportunities.Add("V aktuĂˇlnĂ­m intervalu se neukazuje vĂ˝raznĂ˝ schedule inefficiency kandidĂˇt.");
        }

        var issueCount = orderedInefficiencies.Count(item => item.Severity == CuratedSelectionInefficiencySeverity.Issue);
        var watchCount = orderedInefficiencies.Count(item => item.Severity == CuratedSelectionInefficiencySeverity.Watch);

        var summary = issueCount > 0
            ? $"EMS evaluation detekuje {issueCount} schedule inefficiency issue a {watchCount} watch signĂˇlĹŻ."
            : watchCount > 0
                ? $"EMS evaluation eviduje {watchCount} schedule inefficiency watch signĂˇlĹŻ bez tvrdĂ©ho issue."
                : "EMS evaluation neukazuje vĂ˝raznou schedule inefficiency v aktuĂˇlnĂ­m intervalu.";

        var methodology = "EMS evaluation v1 kombinuje transparentnĂ­ scorecards nad existujĂ­cĂ­mi metrikami: off-hours/active ratio, weekend/weekday ratio, baseload/avg ratio, peak stress ratio a local generation utilization. Bez black-box skĂłre a bez optimalizaÄŤnĂ­ho enginu.";

        if (loadProfile.IsFallback)
        {
            methodology += " Pro krĂˇtkĂ© intervaly pouĹľĂ­vĂˇ load profile fallback snapshot, takĹľe weekend/off-hours signĂˇly majĂ­ niĹľĹˇĂ­ robustnost.";
        }

        return new CuratedSelectionEmsEvaluationSummary
        {
            IsAvailable = true,
            HasMixedSigns = hasMixedSigns,
            HasConsumption = hasConsumption,
            HasGeneration = hasGeneration,
            Summary = summary,
            Methodology = methodology,
            DistinctionNote = "Issue = datovĂ˝/coverage problĂ©m, anomaly = deviation proti baseline, inefficiency = stabilnĂ­ provoznĂ­ neefektivita reĹľimu.",
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

    public async Task<CuratedNodeCompareTimeSeriesResult?> GetCuratedCompareTimeSeriesAsync(
        string primaryNodeKey,
        IEnumerable<string> compareNodeKeys,
        DateTime from,
        DateTime to,
        CuratedNodeTimeSeriesMode mode = CuratedNodeTimeSeriesMode.Auto,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(primaryNodeKey)
            || (!ComparePreviewSupportedNodeKeys.Contains(primaryNodeKey) && !_bindingRegistry.IsSupported(primaryNodeKey)))
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

            if (!ComparePreviewSupportedNodeKeys.Contains(rawNodeKey) && !_bindingRegistry.IsSupported(rawNodeKey))
            {
                excludedNodeMessages.Add($"{rawNodeKey}: compare preview pro tento uzel nenĂ­ v tĂ©to verzi podporovĂˇn.");
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
            excludedNodeMessages.Add($"{primaryNodeKey}: {primaryTimeSeries.NoDataMessage ?? "v analysis window nejsou dostupnĂˇ data"}.");
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
                    excludedNodeMessages.Add($"{nodeKey}: nepodaĹ™ilo se pĹ™ipravit ÄŤasovou Ĺ™adu.");
                    continue;
                }

                if (result.Points.Count == 0)
                {
                    excludedNodeMessages.Add($"{nodeKey}: {result.NoDataMessage ?? "v analysis window nejsou dostupnĂˇ data"}.");
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
            ? "Compare preview nemĂˇ ĹľĂˇdnou dostupnou vĂ˝konovou Ĺ™adu pro vybranĂ© uzly a interval."
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

    public async Task<CuratedNodeDeviationSummary> GetCuratedDeviationSummaryAsync(string nodeKey, DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (nodeKey == "weather_main")
        {
            return new CuratedNodeDeviationSummary
            {
                IsAvailable = false,
                Methodology = "Uzel poÄŤasĂ­ je v tomto sprintu jen vysvÄ›tlujĂ­cĂ­ faktor, deviation baseline se zde zatĂ­m nevyhodnocuje.",
                Message = "Baseline nenĂ­ pro tento uzel / interval zatĂ­m k dispozici.",
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
                Message = "Baseline nenĂ­ pro tento uzel / interval zatĂ­m k dispozici."
            };
        }

        var filePath = ResolveCuratedFilePath(source);
        if (filePath is null)
        {
            return new CuratedNodeDeviationSummary
            {
                IsAvailable = false,
                Methodology = BaselineMethodology,
                Message = "ChybĂ­ lokĂˇlnĂ­ reduced source."
            };
        }

        return await CalculateDeviationAsync(nodeKey, filePath, source, from, to, ct);
    }

    private sealed class CuratedNodeSource
    {
        public required string FileName { get; init; }
        /// <summary>SloĹľka v D:\DataSet\data\ pro binding-based zdroje. Null pro legacy flat CSV.</summary>
        public string? MeterFolder { get; init; }
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

    /// <summary>
    /// VrĂˇtĂ­ CuratedNodeSource pro danĂ˝ nodeKey.
    /// PrimĂˇrnÄ›: vyhledĂˇ v FacilityDataBindingRegistry (novĂ˝ dataset).
    /// Fallback: legacy hardcoded mapping pro starĂ© uzly (pv_main, chp_main, ...).
    /// </summary>
    private CuratedNodeSource? ResolveCuratedNodeSource(string nodeKey)
    {
        // â”€â”€ 1. Binding-based lookup (novĂ˝ dataset) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var binding = _bindingRegistry.GetPrimaryBinding(nodeKey);
        if (binding is not null)
        {
            // UrÄŤenĂ­ signĂˇlnĂ­ho charakteru podle measurement_key
            var isPowerSignal = binding.MeasurementKey is "P" or "P1" or "P2" or "P3";
            var isTempSignal = binding.MeasurementKey is "Ta" or "Trl" or "Tvl" or "Tdiff";
            var isFlowSignal = binding.MeasurementKey is "qv" or "V";

            string unit;
            string summaryLabel;
            string statsUnit;
            string statsLabel;

            if (isPowerSignal)
            {
                unit = "kWh";
                summaryLabel = "IntervalovĂˇ energie";
                statsUnit = "kW";
                statsLabel = "VĂ˝kon";
            }
            else if (isTempSignal)
            {
                unit = "Â°C";
                summaryLabel = "PrĹŻmÄ›rnĂˇ teplota";
                statsUnit = "Â°C";
                statsLabel = "Teplota";
            }
            else if (isFlowSignal)
            {
                unit = "mÂł";
                summaryLabel = "Objem";
                statsUnit = "mÂł";
                statsLabel = "PrĹŻtok";
            }
            else
            {
                unit = binding.Category;   // fallback: category jako jednotka
                summaryLabel = "Hodnota";
                statsUnit = string.Empty;
                statsLabel = "Hodnota";
            }

            // NĂˇzev uzlu: meter_urn je pĹ™irozenĂ˝ identifikĂˇtor pro novĂ˝ dataset
            var title = string.IsNullOrEmpty(binding.MeterUrn) ? nodeKey : binding.MeterUrn;
            // Sloupec v CSV: <meter_urn>.<measurement_key>
            var columnName = $"{binding.MeterUrn}.{binding.MeasurementKey}";

            return new CuratedNodeSource
            {
                FileName           = binding.FileName,
                MeterFolder        = binding.MeterFolder,
                ColumnName         = columnName,
                Title              = title,
                NodeTypeHint       = binding.Category,
                Unit               = unit,
                SummaryLabel       = summaryLabel,
                StatsUnit          = statsUnit,
                StatsLabel         = statsLabel,
                IsPowerSignal      = isPowerSignal,
                PowerToKilowattFactor = isPowerSignal ? 0.001 : 1.0,  // Watts â†’ kW
                SupportsDeviation  = isPowerSignal,   // jen P-signĂˇly majĂ­ baseline overlay
            };
        }

        // â”€â”€ 2. Legacy fallback (starĂ© agregovanĂ© CSV uzly â€” zachovĂˇny pro pĹ™echod) â”€
        return nodeKey switch
        {
            "pv_main" => new CuratedNodeSource
            {
                FileName = "electricity_P.csv",
                ColumnName = "PV",
                Title = "SolĂˇrnĂ­ vĂ˝roba (PV)",
                NodeTypeHint = "generator_pv",
                Unit = "kWh",
                SummaryLabel = "IntervalovĂˇ energie",
                StatsUnit = "kW",
                StatsLabel = "VĂ˝kon",
                IsPowerSignal = true,
                PowerToKilowattFactor = 0.001,
                SupportsDeviation = true
            },
            "chp_main" => new CuratedNodeSource
            {
                FileName = "electricity_P.csv",
                ColumnName = "CHP",
                Title = "VĂ˝roba kogenerace (CHP)",
                NodeTypeHint = "generator_chp",
                Unit = "kWh",
                SummaryLabel = "IntervalovĂˇ energie",
                StatsUnit = "kW",
                StatsLabel = "VĂ˝kon",
                IsPowerSignal = true,
                PowerToKilowattFactor = 0.001,
                SupportsDeviation = true
            },
            "cooling_main" => new CuratedNodeSource
            {
                FileName = "cooling_P.csv",
                ColumnName = "total",
                Title = "CelkovĂ© chlazenĂ­",
                NodeTypeHint = "utility_cooling",
                Unit = "kWh",
                SummaryLabel = "IntervalovĂˇ energie",
                StatsUnit = "kW",
                StatsLabel = "VĂ˝kon",
                IsPowerSignal = true,
                PowerToKilowattFactor = 0.001,
                SupportsDeviation = true
            },
            "heating_main" => new CuratedNodeSource
            {
                FileName = "heating_P.csv",
                ColumnName = "total",
                Title = "CelkovĂ© vytĂˇpÄ›nĂ­",
                NodeTypeHint = "utility_heating",
                Unit = "kWh",
                SummaryLabel = "IntervalovĂˇ energie",
                StatsUnit = "kW",
                StatsLabel = "VĂ˝kon",
                IsPowerSignal = true,
                PowerToKilowattFactor = 0.001,
                SupportsDeviation = true
            },
            "weather_main" => new CuratedNodeSource
            {
                FileName = "weather.csv",
                ColumnName = "WeatherStation.Weather.Ta",
                Title = "OkamĹľitĂˇ prĹŻmÄ›rnĂˇ teplota",
                NodeTypeHint = "weather",
                Unit = "Â°C",
                SummaryLabel = "PrĹŻmÄ›rnĂˇ teplota",
                StatsUnit = "Â°C",
                StatsLabel = "Teplota",
                SupportsDeviation = false
            },
            _ => null
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
                Message = "NeplatnĂ˝ ÄŤasovĂ˝ rozsah pro vĂ˝poÄŤet baseline."
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
                Message = "Baseline nelze pĹ™ipravit: chybĂ­ referenÄŤnĂ­ baseline okna."
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
                    Message = "Reduced source je prĂˇzdnĂ˝."
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
                    Message = "Reduced source neobsahuje oÄŤekĂˇvanĂ˝ sloupec pro vybranĂ˝ uzel."
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
                    Message = "Pro vybranĂ˝ interval nejsou k dispozici ĹľĂˇdnĂˇ data."
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
                    Message = "Baseline nelze spoÄŤĂ­tat: nejsou dostupnĂˇ referenÄŤnĂ­ baseline okna s dostateÄŤnĂ˝m pokrytĂ­m dat."
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
                    Message = "Baseline je pro tento interval pĹ™Ă­liĹˇ nĂ­zkĂˇ pro stabilnĂ­ procentnĂ­ vyhodnocenĂ­ odchylky."
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
                Message = "NepodaĹ™ilo se naÄŤĂ­st reduced source pro baseline vĂ˝poÄŤet."
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
                $"StejnĂ© obdobĂ­ minulĂ©ho roku (-{yearOffset}y)"));
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
                $"PĹ™edchozĂ­ srovnatelnĂ© obdobĂ­ (-{offsetIndex}x interval)"));
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
                "StejnĂ© obdobĂ­ v minulĂ˝ch letech (mediĂˇn)",
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
                "Hybrid: stejnĂ© obdobĂ­ minulĂ˝ rok + recent comparable obdobĂ­",
                referenceCandidates
            );
        }

        if (samePeriodReferenceAggregates.Count == 1)
        {
            return new BaselineSelection(
                samePeriodReferenceAggregates[0].Sum,
                "StejnĂ© obdobĂ­ minulĂ˝ rok",
                [samePeriodReferenceAggregates[0].Candidate]
            );
        }

        if (recentComparableReferenceAggregates.Count >= 2)
        {
            return new BaselineSelection(
                Median(recentComparableReferenceAggregates.Select(x => x.Sum).ToList()),
                "PĹ™edchozĂ­ srovnatelnĂˇ obdobĂ­ stejnĂ© dĂ©lky (mediĂˇn)",
                recentComparableReferenceAggregates.Select(x => x.Candidate).ToList()
            );
        }

        if (recentComparableReferenceAggregates.Count == 1)
        {
            return new BaselineSelection(
                recentComparableReferenceAggregates[0].Sum,
                "BezprostĹ™ednÄ› pĹ™edchozĂ­ srovnatelnĂ© obdobĂ­",
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

        // U velmi nĂ­zkĂ© baseline je procentnĂ­ odchylka numericky nestabilnĂ­.
        return Math.Max(0.5, intervalDuration.TotalHours * 0.05);
    }

    private static string BuildMethodologyText(string strategyDescription, DateTime from, DateTime to, int minimumReferenceSamples)
    {
        return $"{BaselineMethodology} Strategy: {strategyDescription}. Analysis window: {from:u} - {to:u}. Minimum pokrytĂ­ referenÄŤnĂ­ho okna: {minimumReferenceSamples} vzorkĹŻ.";
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
            return CreateUnavailableWeatherExplanation("VysvÄ›tlenĂ­ poÄŤasĂ­m nenĂ­ dostupnĂ©, protoĹľe chybĂ­ weather.csv source.");
        }

        var weatherAverages = await GetWeatherAveragesAsync(weatherFilePath, analysisFrom, analysisTo, referenceCandidates, minimumReferenceSamples, ct);
        if (!weatherAverages.CurrentAverageTempC.HasValue)
        {
            return CreateUnavailableWeatherExplanation("VysvÄ›tlenĂ­ poÄŤasĂ­m nenĂ­ dostupnĂ©: v aktuĂˇlnĂ­m intervalu chybĂ­ data venkovnĂ­ teploty.");
        }

        if (weatherAverages.ReferenceAverageTempC.Count == 0)
        {
            return CreateUnavailableWeatherExplanation("VysvÄ›tlenĂ­ poÄŤasĂ­m nenĂ­ dostupnĂ©: v referenÄŤnĂ­m baseline obdobĂ­ chybĂ­ dostatek weather dat.");
        }

        var referenceAverage = weatherAverages.ReferenceAverageTempC.Count >= 2
            ? Median(weatherAverages.ReferenceAverageTempC)
            : weatherAverages.ReferenceAverageTempC[0];
        var currentAverage = weatherAverages.CurrentAverageTempC.Value;
        var deltaTemp = currentAverage - referenceAverage;

        var status = WeatherExplanationStatus.NotSupportedByWeather;
        var conclusion = "PoÄŤasĂ­ odchylku v tomto intervalu nepodporuje.";

        if (Math.Abs(deltaTemp) < WeatherExplanationDeltaThresholdC)
        {
            return new WeatherExplanationSummary
            {
                IsAvailable = true,
                Status = WeatherExplanationStatus.WeatherChangeNeutral,
                CurrentAverageOutdoorTempC = currentAverage,
                ReferenceAverageOutdoorTempC = referenceAverage,
                DeltaOutdoorTempC = deltaTemp,
                Conclusion = "RozdĂ­l poÄŤasĂ­ vĹŻÄŤi referenci je malĂ˝.",
                Methodology = "Explanatory heuristika v1: porovnĂˇnĂ­ prĹŻmÄ›rnĂ© venkovnĂ­ teploty v analysis window vĹŻÄŤi referenÄŤnĂ­m baseline oknĹŻm. PĹ™i |delta T| < 0.8 Â°C je zmÄ›na poÄŤasĂ­ povaĹľovĂˇna za malou."
            };
        }

        if (nodeKey == "heating_main")
        {
            var isColderThanReference = deltaTemp <= -WeatherExplanationDeltaThresholdC;
            var isWarmerThanReference = deltaTemp >= WeatherExplanationDeltaThresholdC;

            if ((deltaAbsolute > 0 && isColderThanReference) || (deltaAbsolute < 0 && isWarmerThanReference))
            {
                status = WeatherExplanationStatus.SupportedByWeather;
                conclusion = "Odchylka mĹŻĹľe bĂ˝t ÄŤĂˇsteÄŤnÄ› vysvÄ›tlena poÄŤasĂ­m.";
            }
        }
        else if (nodeKey == "cooling_main")
        {
            var isWarmerThanReference = deltaTemp >= WeatherExplanationDeltaThresholdC;
            var isColderThanReference = deltaTemp <= -WeatherExplanationDeltaThresholdC;

            if ((deltaAbsolute > 0 && isWarmerThanReference) || (deltaAbsolute < 0 && isColderThanReference))
            {
                status = WeatherExplanationStatus.SupportedByWeather;
                conclusion = "Odchylka mĹŻĹľe bĂ˝t ÄŤĂˇsteÄŤnÄ› vysvÄ›tlena poÄŤasĂ­m.";
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
            Methodology = "Explanatory heuristika v1: porovnĂˇnĂ­ prĹŻmÄ›rnĂ© venkovnĂ­ teploty v analysis window vĹŻÄŤi referenÄŤnĂ­m baseline oknĹŻm pouĹľitĂ˝m baseline strategiĂ­."
        };
    }

    private static WeatherExplanationSummary CreateUnavailableWeatherExplanation(string message)
    {
        return new WeatherExplanationSummary
        {
            IsAvailable = false,
            Status = WeatherExplanationStatus.Unavailable,
            Conclusion = message,
            Methodology = "Explanatory heuristika vyĹľaduje dostupnĂˇ weather data pro aktuĂˇlnĂ­ i referenÄŤnĂ­ obdobĂ­."
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
        var valueKind = source.IsPowerSignal ? "vĂ˝konu" : "hodnoty";

        if (requestedMode == CuratedNodeTimeSeriesMode.Raw15Min)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.Raw15Min,
                source.IsPowerSignal ? "Raw 15min vĂ˝kon" : "Raw detailnĂ­ Ĺ™ada",
                "RuÄŤnĂ­ reĹľim 15min: bez agregace, zobrazeny jsou pĹŻvodnĂ­ vzorky ÄŤasovĂ© Ĺ™ady.",
                CuratedNodeTimeSeriesMode.Raw15Min,
                "15min"
            );
        }

        if (requestedMode == CuratedNodeTimeSeriesMode.HourlyAverage)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.HourlyAverage,
                source.IsPowerSignal ? "HodinovĂ˝ prĹŻmÄ›r vĂ˝konu" : "HodinovĂ˝ prĹŻmÄ›r hodnoty",
                $"RuÄŤnĂ­ reĹľim Hourly: kaĹľdĂˇ hodnota je aritmetickĂ˝ prĹŻmÄ›r {valueKind} v danĂ©m hodinovĂ©m bucketu.",
                CuratedNodeTimeSeriesMode.HourlyAverage,
                "Hourly"
            );
        }

        if (requestedMode == CuratedNodeTimeSeriesMode.DailyAverage)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.DailyAverage,
                source.IsPowerSignal ? "DennĂ­ prĹŻmÄ›r vĂ˝konu" : "DennĂ­ prĹŻmÄ›r hodnoty",
                $"RuÄŤnĂ­ reĹľim Daily: kaĹľdĂˇ hodnota je aritmetickĂ˝ prĹŻmÄ›r {valueKind} v danĂ©m dennĂ­m bucketu.",
                CuratedNodeTimeSeriesMode.DailyAverage,
                "Daily"
            );
        }

        if (duration <= RawTimeSeriesThreshold)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.Raw15Min,
                source.IsPowerSignal ? "Raw 15min vĂ˝kon" : "Raw detailnĂ­ Ĺ™ada",
                "Auto reĹľim: bez agregace, zobrazeny jsou pĹŻvodnĂ­ vzorky ÄŤasovĂ© Ĺ™ady.",
                CuratedNodeTimeSeriesMode.Auto,
                "Auto"
            );
        }

        if (duration <= HourlyTimeSeriesThreshold)
        {
            return new TimeSeriesGranularityDecision(
                CuratedNodeTimeSeriesGranularity.HourlyAverage,
                source.IsPowerSignal ? "HodinovĂ˝ prĹŻmÄ›r vĂ˝konu" : "HodinovĂ˝ prĹŻmÄ›r hodnoty",
                $"Auto reĹľim: agregace po hodinĂˇch, kaĹľdĂˇ hodnota je aritmetickĂ˝ prĹŻmÄ›r {valueKind} v danĂ©m hodinovĂ©m bucketu.",
                CuratedNodeTimeSeriesMode.Auto,
                "Auto"
            );
        }

        return new TimeSeriesGranularityDecision(
            CuratedNodeTimeSeriesGranularity.DailyAverage,
            source.IsPowerSignal ? "DennĂ­ prĹŻmÄ›r vĂ˝konu" : "DennĂ­ prĹŻmÄ›r hodnoty",
            $"Auto reĹľim: agregace po dnech, kaĹľdĂˇ hodnota je aritmetickĂ˝ prĹŻmÄ›r {valueKind} v danĂ©m dennĂ­m bucketu.",
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
                CuratedNodeTimeSeriesGranularity.HourlyAverage => "Graf zobrazuje hodinovÄ› agregovanĂ˝ prĹŻmÄ›r vĂ˝konu (kW). Souhrn nad grafem zĹŻstĂˇvĂˇ intervalovĂˇ energie (kWh).",
                CuratedNodeTimeSeriesGranularity.DailyAverage => "Graf zobrazuje dennÄ› agregovanĂ˝ prĹŻmÄ›r vĂ˝konu (kW). Souhrn nad grafem zĹŻstĂˇvĂˇ intervalovĂˇ energie (kWh).",
                _ => "Graf zobrazuje okamĹľitĂ˝ vĂ˝kon v ÄŤase (kW) v pĹŻvodnĂ­m kroku ÄŤasovĂ© Ĺ™ady (~15 min). Souhrn nad grafem zobrazuje intervalovou energii (kWh)."
            };
        }

        return granularity.Granularity switch
        {
            CuratedNodeTimeSeriesGranularity.HourlyAverage => "Graf zobrazuje hodinovÄ› agregovanĂ˝ prĹŻmÄ›r hodnoty pro vybranĂ˝ uzel.",
            CuratedNodeTimeSeriesGranularity.DailyAverage => "Graf zobrazuje dennÄ› agregovanĂ˝ prĹŻmÄ›r hodnoty pro vybranĂ˝ uzel.",
            _ => "Graf zobrazuje okamĹľitou hodnotu v ÄŤase pro vybranĂ˝ uzel."
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
            CuratedNodeTimeSeriesGranularity.HourlyAverage => "Compare preview zobrazuje vĂ˝konovĂ© Ĺ™ady v ÄŤase (kW) pro vĂ­ce uzlĹŻ. HodinovĂˇ agregace znamenĂˇ prĹŻmÄ›r vĂ˝konu za hodinovĂ˝ bucket.",
            CuratedNodeTimeSeriesGranularity.DailyAverage => "Compare preview zobrazuje vĂ˝konovĂ© Ĺ™ady v ÄŤase (kW) pro vĂ­ce uzlĹŻ. DennĂ­ agregace znamenĂˇ prĹŻmÄ›r vĂ˝konu za dennĂ­ bucket.",
            _ => "Compare preview zobrazuje vĂ˝konovĂ© Ĺ™ady v ÄŤase (kW) pro vĂ­ce uzlĹŻ v pĹŻvodnĂ­m detailnĂ­m kroku dat (~15 min)."
        };
    }

    private static string ResolveSelectionAggregateInterpretationNote(CuratedNodeTimeSeriesGranularity granularity, CuratedAggregateEnergyProfile energyProfile)
    {
        var signedSemanticsNote = energyProfile switch
        {
            CuratedAggregateEnergyProfile.MixedSigned => " Selection kombinuje spotĹ™ebu i vĂ˝robu: kladnĂ© body reprezentujĂ­ ÄŤistou spotĹ™ebu, zĂˇpornĂ© body ÄŤistou vĂ˝robu/export.",
            CuratedAggregateEnergyProfile.GenerationOnly => " Selection je vĂ˝robnĂ­/exportnĂ­: zĂˇpornĂ© body jsou oÄŤekĂˇvanĂ© a reprezentujĂ­ export nebo vĂ˝robu.",
            _ => string.Empty
        };

        return granularity switch
        {
            CuratedNodeTimeSeriesGranularity.HourlyAverage => "AgregovanĂ˝ graf zobrazuje souÄŤet hodinovĂ˝ch prĹŻmÄ›rĹŻ vĂ˝konu (kW) pĹ™es podporovanĂ© uzly selection setu." + signedSemanticsNote,
            CuratedNodeTimeSeriesGranularity.DailyAverage => "AgregovanĂ˝ graf zobrazuje souÄŤet dennĂ­ch prĹŻmÄ›rĹŻ vĂ˝konu (kW) pĹ™es podporovanĂ© uzly selection setu." + signedSemanticsNote,
            _ => "AgregovanĂ˝ graf zobrazuje souÄŤet okamĹľitĂ˝ch vĂ˝konĹŻ (kW) pĹ™es podporovanĂ© uzly selection setu v pĹŻvodnĂ­m kroku dat (~15 min)." + signedSemanticsNote
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
            0 => "Disaggregation foundation: v intervalu nejsou dostupnĂ­ measured contributoĹ™i.",
            1 => $"Aggregate je sloĹľen z 1 measured contributoru: {measuredContributors[0].Label}.",
            _ when hasMixedSigns => $"Aggregate je sloĹľen z {measuredContributors.Count} measured contributoru (load {consumptionContributorCount}, generation {generationContributorCount}).",
            _ when energyProfile == CuratedAggregateEnergyProfile.GenerationOnly => $"Aggregate je sloĹľen z {measuredContributors.Count} measured contributoru, kteri snizuji netto bilanci (generation/export).",
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
                (netEnergyKwh, "Netto bilance energie", "Selection kombinuje spotĹ™ebu i vĂ˝robu; hlavnĂ­ KPI proto reprezentuje netto vĂ˝sledek.", true),
            CuratedAggregateEnergyProfile.GenerationOnly =>
                (totalGenerationKwh, "CelkovĂˇ vĂ˝roba energie", "Selection je ÄŤistÄ› vĂ˝robnĂ­/exportnĂ­; netto bilance je zĂˇpornĂˇ.", false),
            CuratedAggregateEnergyProfile.ConsumptionOnly =>
                (totalConsumptionKwh, "CelkovĂˇ spotĹ™eba energie", "Selection je ÄŤistÄ› spotĹ™ebnĂ­; netto bilance odpovĂ­dĂˇ spotĹ™ebÄ›.", false),
            _ =>
                (netEnergyKwh, "Netto bilance energie", "Selection nemĂˇ v intervalu vĂ˝znamnĂ© energetickĂ© contribution.", true)
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
            using var reader = OpenCsvReader(filePath);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(headerLine))
            {
                return CreateResult(
                    [],
                    "Reduced source je prĂˇzdnĂ˝.",
                    [],
                    includeBaselineOverlay ? "Baseline overlay nenĂ­ dostupnĂ˝: reduced source je prĂˇzdnĂ˝." : null);
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
                    "Reduced source neobsahuje oÄŤekĂˇvanĂ˝ sloupec pro vybranĂ˝ uzel.",
                    [],
                    includeBaselineOverlay ? "Baseline overlay nenĂ­ dostupnĂ˝: reduced source neobsahuje oÄŤekĂˇvanĂ˝ sloupec." : null);
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
                    baselineOverlayMessage = "Baseline overlay nenĂ­ pro tento uzel podporovĂˇn.";
                }
                else if (baselineCandidates.Count == 0)
                {
                    baselineOverlayMessage = "Baseline overlay nenĂ­ dostupnĂ˝: chybĂ­ referenÄŤnĂ­ baseline okna.";
                }
                else if (currentIntervalSamples == 0)
                {
                    baselineOverlayMessage = "Baseline overlay nelze spoÄŤĂ­tat, protoĹľe v analysis window chybĂ­ aktuĂˇlnĂ­ vzorky.";
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
                        baselineOverlayMessage = "Baseline overlay nenĂ­ dostupnĂ˝: referenÄŤnĂ­ baseline okna nemajĂ­ dostateÄŤnĂ© pokrytĂ­ dat.";
                    }
                    else
                    {
                        baselinePoints = BuildBaselineOverlaySeries(selectedBaseline, baselineBucketStatsByCandidate);
                        if (baselinePoints.Count == 0)
                        {
                            baselineOverlayMessage = "Baseline overlay nenĂ­ dostupnĂ˝: referenÄŤnĂ­ Ĺ™ada pro vybranou granularitu nemĂˇ ĹľĂˇdnĂ© body.";
                        }
                    }
                }
            }

            return CreateResult(
                points,
                points.Count == 0 ? "Pro vybranĂ˝ interval nejsou k dispozici ĹľĂˇdnĂ© body ÄŤasovĂ© Ĺ™ady." : null,
                baselinePoints,
                baselineOverlayMessage);
        }
        catch
        {
            return CreateResult(
                [],
                "NepodaĹ™ilo se naÄŤĂ­st reduced source pro ÄŤasovou Ĺ™adu.",
                [],
                includeBaselineOverlay ? "Baseline overlay nenĂ­ dostupnĂ˝: nepodaĹ™ilo se naÄŤĂ­st reduced source." : null);
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
