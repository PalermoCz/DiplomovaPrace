using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace DiplomovaPrace.Services;

/// <summary>
/// Singleton registry pro dataset_bindings_fixed.csv.
/// Načte se jednou při startu aplikace a poskytuje:
/// - lookup node_id → seznam binding záznamů
/// - výběr primárního signálu pro node (P@15min preferred)
/// - resolving absolutní cesty k datovým souborům (DataRootPath/meterFolder/fileName)
///
/// Umožňuje NodeAnalyticsPreviewService přejít z hardcoded 5 uzlů
/// na plný binding-based dataset s 81 meter uzly.
/// </summary>
public sealed class FacilityDataBindingRegistry
{
    /// <summary>Jeden řádek z dataset_bindings_fixed.csv.</summary>
    public sealed class BindingRecord
    {
        public string NodeId { get; init; } = string.Empty;
        public string MeterUrn { get; init; } = string.Empty;
        public string MeterFolder { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public string MeasurementKey { get; init; } = string.Empty;
        public string DataStage { get; init; } = string.Empty;
        public string Resolution { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public FacilitySignalCode ExactSignalCode => FacilitySignalTaxonomy.NormalizeExactCode(MeasurementKey);
        public FacilitySignalFamily SignalFamily => FacilitySignalTaxonomy.ResolveFamily(ExactSignalCode);
    }

    private readonly IReadOnlyDictionary<string, IReadOnlyList<BindingRecord>> _byNodeId;
    private readonly string _dataRootPath;

    public FacilityDataBindingRegistry(IConfiguration config, ILogger<FacilityDataBindingRegistry> logger)
    {
        _dataRootPath = config["Facility:DataRootPath"] ?? string.Empty;
        var bindingsCsvPath = config["Facility:BindingsCsvPath"];
        _byNodeId = LoadBindings(bindingsCsvPath, logger);

        logger.LogInformation(
            "FacilityDataBindingRegistry: loaded {Count} node binding entries from '{Path}', DataRootPath='{DataRoot}'",
            _byNodeId.Count, bindingsCsvPath ?? "(not configured)", _dataRootPath);
    }

    /// <summary>Vrátí všechny binding záznamy pro daný node_id.</summary>
    public IReadOnlyList<BindingRecord> GetBindings(string nodeId)
        => _byNodeId.TryGetValue(nodeId, out var list) ? list : [];

    public IReadOnlyList<BindingRecord> GetBindings(string nodeId, FacilitySignalCode exactSignalCode)
        => FilterBindings(nodeId, binding => FacilitySignalTaxonomy.MatchesExactCode(binding.ExactSignalCode, exactSignalCode));

    public IReadOnlyList<BindingRecord> GetBindings(string nodeId, FacilitySignalFamily signalFamily)
        => FilterBindings(nodeId, binding => binding.SignalFamily == signalFamily);

    /// <summary>
    /// Vrátí preferovaný binding pro node.
    /// Priorita: P@15min → P → Ta@15min → Ta → jakýkoliv@15min → první dostupný.
    /// </summary>
    public BindingRecord? GetPrimaryBinding(string nodeId)
    {
        if (!_byNodeId.TryGetValue(nodeId, out var bindings) || bindings.Count == 0)
            return null;

        return SelectPreferredBinding(bindings);
    }

    public BindingRecord? GetPreferredBinding(string nodeId, FacilitySignalCode exactSignalCode)
        => SelectPreferredBinding(GetBindings(nodeId, exactSignalCode));

    public BindingRecord? GetPreferredBinding(string nodeId, FacilitySignalFamily signalFamily)
        => SelectPreferredBinding(GetBindings(nodeId, signalFamily));

    /// <summary>Vrátí true pokud pro node existuje alespoň jedna vazba.</summary>
    public bool IsSupported(string nodeId)
        => _byNodeId.ContainsKey(nodeId);

    /// <summary>Množina všech podporovaných node_id.</summary>
    public IReadOnlyCollection<string> GetSupportedNodeIds()
        => _byNodeId.Keys.ToList();

    /// <summary>
    /// Sestaví absolutní cestu DataRootPath/meterFolder/fileName.
    /// Vrátí null pokud cesta neexistuje nebo DataRootPath není nakonfigurováno.
    /// </summary>
    public string? ResolveFilePath(string meterFolder, string fileName)
    {
        if (string.IsNullOrEmpty(_dataRootPath)) return null;
        var path = Path.GetFullPath(Path.Combine(_dataRootPath, meterFolder, fileName));
        return File.Exists(path) ? path : null;
    }

    private IReadOnlyList<BindingRecord> FilterBindings(string nodeId, Func<BindingRecord, bool> predicate)
    {
        if (!_byNodeId.TryGetValue(nodeId, out var bindings) || bindings.Count == 0)
        {
            return [];
        }

        return bindings.Where(predicate).ToList();
    }

    private static BindingRecord? SelectPreferredBinding(IEnumerable<BindingRecord> bindings)
    {
        var bindingList = bindings as IReadOnlyList<BindingRecord> ?? bindings.ToList();
        if (bindingList.Count == 0)
        {
            return null;
        }

        return bindingList.FirstOrDefault(binding => FacilitySignalTaxonomy.MatchesExactCode(binding.ExactSignalCode, FacilitySignalCode.P) && IsPreferredResolution(binding))
            ?? bindingList.FirstOrDefault(binding => FacilitySignalTaxonomy.MatchesExactCode(binding.ExactSignalCode, FacilitySignalCode.P))
            ?? bindingList.FirstOrDefault(binding => FacilitySignalTaxonomy.MatchesExactCode(binding.ExactSignalCode, FacilitySignalCode.Ta) && IsPreferredResolution(binding))
            ?? bindingList.FirstOrDefault(binding => FacilitySignalTaxonomy.MatchesExactCode(binding.ExactSignalCode, FacilitySignalCode.Ta))
            ?? bindingList.FirstOrDefault(IsPreferredResolution)
            ?? bindingList[0];
    }

    private static bool IsPreferredResolution(BindingRecord binding)
        => string.Equals(binding.Resolution, "15min", StringComparison.OrdinalIgnoreCase);

    // ── Načítání CSV ──────────────────────────────────────────────────────────

    private static IReadOnlyDictionary<string, IReadOnlyList<BindingRecord>> LoadBindings(
        string? csvPath, ILogger logger)
    {
        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
        {
            logger.LogWarning("FacilityDataBindingRegistry: bindings CSV nebyl nalezen na '{Path}'", csvPath ?? "(null)");
            return new Dictionary<string, IReadOnlyList<BindingRecord>>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
            });

            var records = csv.GetRecords<BindingCsvRow>().ToList();

            return records
                .GroupBy(r => r.NodeId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<BindingRecord>)g
                        .Select(r => new BindingRecord
                        {
                            NodeId         = r.NodeId,
                            MeterUrn       = r.MeterUrn,
                            MeterFolder    = r.MeterFolder,
                            FileName       = r.FileName,
                            MeasurementKey = r.MeasurementKey,
                            DataStage      = r.DataStage,
                            Resolution     = r.Resolution,
                            Category       = r.Category,
                        })
                        .ToList(),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FacilityDataBindingRegistry: chyba při načítání bindings z '{Path}'", csvPath);
            return new Dictionary<string, IReadOnlyList<BindingRecord>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    // ── Interní CSV row model ─────────────────────────────────────────────────

    private sealed class BindingCsvRow
    {
        [CsvHelper.Configuration.Attributes.Name("node_id")]
        public string NodeId { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("meter_urn")]
        public string MeterUrn { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("meter_folder")]
        public string MeterFolder { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("file_name")]
        public string FileName { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("measurement_key")]
        public string MeasurementKey { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("data_stage")]
        public string DataStage { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("resolution")]
        public string Resolution { get; set; } = string.Empty;

        [CsvHelper.Configuration.Attributes.Name("category")]
        public string Category { get; set; } = string.Empty;
    }
}
