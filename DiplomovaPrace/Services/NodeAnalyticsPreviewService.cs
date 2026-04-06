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
}

public class NodeAnalyticsPreviewService
{
    private const string BaselineMethodology = "Analysis window je vždy přímo vybraný interval uživatele. Baseline strategy je určena separátně: priorita je stejné období v minulých letech, fallback jsou předchozí srovnatelná období stejné délky. U řad s příponou P se před součtem převádí výkon na energii podle kroku časové řady (inferovaný krok, fallback 15 min).";
    private const double DefaultPowerSampleStepHours = 0.25;
    private const int MaxHistoricalYearsForBaseline = 3;
    private const int RecentComparableWindowsForBaseline = 4;
    private const double MinimumReferenceCoverageRatio = 0.60;

    private readonly IKpiService _kpiService;
    private readonly IWebHostEnvironment _env;
    private readonly ConcurrentDictionary<string, DateTime> _maxTimestampCache = new(StringComparer.OrdinalIgnoreCase);

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

        return await CalculateDeviationAsync(filePath, source, from, to, ct);
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

    private async Task<CuratedNodeDeviationSummary> CalculateDeviationAsync(string filePath, CuratedNodeSource source, DateTime from, DateTime to, CancellationToken ct)
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

            var samePeriodReferenceSums = baselineCandidates
                .Where(candidate => candidate.Kind == BaselineReferenceKind.SamePeriodPreviousYear)
                .Select(candidate => baselineStats[candidate])
                .Where(stats => stats.Count >= minimumReferenceSamples)
                .Select(stats => stats.Sum)
                .ToList();

            var recentComparableReferenceSums = baselineCandidates
                .Where(candidate => candidate.Kind == BaselineReferenceKind.RecentComparablePeriod)
                .Select(candidate => baselineStats[candidate])
                .Where(stats => stats.Count >= minimumReferenceSamples)
                .Select(stats => stats.Sum)
                .ToList();

            var selectedBaseline = SelectBaselineValue(samePeriodReferenceSums, recentComparableReferenceSums);
            if (!selectedBaseline.HasValue)
            {
                return new CuratedNodeDeviationSummary
                {
                    IsAvailable = false,
                    Unit = source.Unit,
                    Methodology = BaselineMethodology,
                    Message = "Baseline nelze spočítat: nejsou dostupná referenční baseline okna s dostatečným pokrytím dat."
                };
            }

            var baselineValue = selectedBaseline.Value.Value;
            var minimumMeaningfulBaseline = GetMinimumMeaningfulBaseline(intervalDuration, source);
            if (Math.Abs(baselineValue) < minimumMeaningfulBaseline)
            {
                return new CuratedNodeDeviationSummary
                {
                    IsAvailable = false,
                    Unit = source.Unit,
                    Methodology = BuildMethodologyText(selectedBaseline.Value.StrategyDescription, from, to, minimumReferenceSamples),
                    Message = "Baseline je pro tento interval příliš nízká pro stabilní procentní vyhodnocení odchylky."
                };
            }

            var currentValue = currentStats.Sum;
            var deltaAbsolute = currentValue - baselineValue;
            var deltaPercent = (deltaAbsolute / baselineValue) * 100.0;

            return new CuratedNodeDeviationSummary
            {
                IsAvailable = true,
                CurrentValue = currentValue,
                BaselineValue = baselineValue,
                DeltaAbsolute = deltaAbsolute,
                DeltaPercent = deltaPercent,
                Severity = ClassifySeverity(deltaPercent),
                ReferenceIntervalsUsed = selectedBaseline.Value.ReferenceIntervalsUsed,
                Unit = source.Unit,
                Methodology = BuildMethodologyText(selectedBaseline.Value.StrategyDescription, from, to, minimumReferenceSamples)
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
            var baselineFrom = from.AddYears(-yearOffset);
            var baselineTo = to.AddYears(-yearOffset);
            candidates.Add(new BaselineCandidate(
                baselineFrom,
                baselineTo,
                BaselineReferenceKind.SamePeriodPreviousYear,
                $"Stejné období minulého roku (-{yearOffset}y)"));
        }

        for (var offsetIndex = 1; offsetIndex <= RecentComparableWindowsForBaseline; offsetIndex++)
        {
            var offsetTicks = duration.Ticks * offsetIndex;
            var offset = TimeSpan.FromTicks(offsetTicks);
            var baselineFrom = from - offset;
            var baselineTo = to - offset;
            candidates.Add(new BaselineCandidate(
                baselineFrom,
                baselineTo,
                BaselineReferenceKind.RecentComparablePeriod,
                $"Předchozí srovnatelné období (-{offsetIndex}x interval)"));
        }

        return candidates;
    }

    private static (double Value, int ReferenceIntervalsUsed, string StrategyDescription)? SelectBaselineValue(
        IReadOnlyList<double> samePeriodReferenceSums,
        IReadOnlyList<double> recentComparableReferenceSums)
    {
        if (samePeriodReferenceSums.Count >= 2)
        {
            return (
                Median(samePeriodReferenceSums),
                samePeriodReferenceSums.Count,
                "Stejné období v minulých letech (medián)"
            );
        }

        if (samePeriodReferenceSums.Count == 1 && recentComparableReferenceSums.Count >= 2)
        {
            var samePeriod = samePeriodReferenceSums[0];
            var recentMedian = Median(recentComparableReferenceSums);
            return (
                (samePeriod * 0.70) + (recentMedian * 0.30),
                1 + recentComparableReferenceSums.Count,
                "Hybrid: stejné období minulý rok + recent comparable období"
            );
        }

        if (samePeriodReferenceSums.Count == 1)
        {
            return (
                samePeriodReferenceSums[0],
                1,
                "Stejné období minulý rok"
            );
        }

        if (recentComparableReferenceSums.Count >= 2)
        {
            return (
                Median(recentComparableReferenceSums),
                recentComparableReferenceSums.Count,
                "Předchozí srovnatelná období stejné délky (medián)"
            );
        }

        if (recentComparableReferenceSums.Count == 1)
        {
            return (
                recentComparableReferenceSums[0],
                1,
                "Bezprostředně předchozí srovnatelné období"
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
