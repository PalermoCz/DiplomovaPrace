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
