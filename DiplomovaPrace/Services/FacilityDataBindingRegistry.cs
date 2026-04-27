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
        public string BindingId { get; init; } = string.Empty;
        public string NodeId { get; init; } = string.Empty;
        public string MeterUrn { get; init; } = string.Empty;
        public string MeterFolder { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public string MeasurementKey { get; init; } = string.Empty;
        public string DataStage { get; init; } = string.Empty;
        public string Resolution { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string Unit { get; init; } = string.Empty;
        public string? SourceLabel { get; init; }
        public string? OriginalFileName { get; init; }
        public string? SourceFilePath { get; init; }
        public DateTime? ImportedUtc { get; init; }
        public bool UsesFixedCsvSeriesFormat { get; init; }
        public FacilitySignalCode ExactSignalCode => FacilitySignalTaxonomy.NormalizeExactCode(MeasurementKey);
        public FacilitySignalFamily SignalFamily => FacilitySignalTaxonomy.ResolveFamily(ExactSignalCode);
    }

    private readonly IReadOnlyDictionary<string, IReadOnlyList<BindingRecord>> _seedByNodeId;
    private readonly Dictionary<string, BindingRecord> _importedBindingsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncRoot = new();
    private readonly string _contentRootPath;
    private readonly string _dataRootPath;

    public FacilityDataBindingRegistry(
        IConfiguration config,
        IWebHostEnvironment environment,
        FacilityEditorStateService editorStateService,
        ILogger<FacilityDataBindingRegistry> logger)
    {
        _contentRootPath = environment.ContentRootPath;
        _dataRootPath = config["Facility:DataRootPath"] ?? string.Empty;
        var bindingsCsvPath = config["Facility:BindingsCsvPath"];
        _seedByNodeId = LoadBindings(bindingsCsvPath, logger);

        foreach (var binding in LoadImportedBindings(editorStateService, _contentRootPath, logger))
        {
            _importedBindingsById[binding.BindingId] = binding;
        }

        logger.LogInformation(
            "FacilityDataBindingRegistry: loaded {SeedCount} seeded nodes and {ImportedCount} imported bindings from '{Path}', DataRootPath='{DataRoot}'",
            _seedByNodeId.Count,
            _importedBindingsById.Count,
            bindingsCsvPath ?? "(not configured)",
            _dataRootPath);
    }

    /// <summary>Vrátí všechny binding záznamy pro daný node_id.</summary>
    public IReadOnlyList<BindingRecord> GetBindings(string nodeId)
        => GetBindingsCore(nodeId);

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
        var bindings = GetBindingsCore(nodeId);
        if (bindings.Count == 0)
            return null;

        return SelectPreferredBinding(bindings);
    }

    public BindingRecord? GetPreferredBinding(string nodeId, FacilitySignalCode exactSignalCode)
        => SelectPreferredBinding(GetBindings(nodeId, exactSignalCode));

    public BindingRecord? GetPreferredBinding(string nodeId, FacilitySignalFamily signalFamily)
        => SelectPreferredBinding(GetBindings(nodeId, signalFamily));

    /// <summary>Vrátí true pokud pro node existuje alespoň jedna vazba.</summary>
    public bool IsSupported(string nodeId)
        => GetBindingsCore(nodeId).Count > 0;

    /// <summary>Množina všech podporovaných node_id.</summary>
    public IReadOnlyCollection<string> GetSupportedNodeIds()
    {
        var supported = new HashSet<string>(_seedByNodeId.Keys, StringComparer.OrdinalIgnoreCase);

        lock (_syncRoot)
        {
            foreach (var binding in _importedBindingsById.Values)
            {
                if (!string.IsNullOrWhiteSpace(binding.NodeId))
                {
                    supported.Add(binding.NodeId);
                }
            }
        }

        return supported.ToList();
    }

    public void UpsertImportedBinding(FacilityImportedBindingState state, string absoluteFilePath)
    {
        var binding = CreateImportedBindingRecord(state, absoluteFilePath, _contentRootPath);

        lock (_syncRoot)
        {
            _importedBindingsById[binding.BindingId] = binding;
        }
    }

    public bool RemoveImportedBinding(string? bindingId)
    {
        var normalizedBindingId = bindingId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedBindingId))
        {
            return false;
        }

        lock (_syncRoot)
        {
            return _importedBindingsById.Remove(normalizedBindingId);
        }
    }

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
        var bindings = GetBindingsCore(nodeId);
        if (bindings.Count == 0)
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

    private IReadOnlyList<BindingRecord> GetBindingsCore(string? nodeId)
    {
        var normalizedNodeId = nodeId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedNodeId))
        {
            return [];
        }

        var result = new List<BindingRecord>();

        lock (_syncRoot)
        {
            result.AddRange(_importedBindingsById.Values.Where(binding =>
                binding.NodeId.Equals(normalizedNodeId, StringComparison.OrdinalIgnoreCase)));
        }

        if (_seedByNodeId.TryGetValue(normalizedNodeId, out var seededBindings) && seededBindings.Count > 0)
        {
            result.AddRange(seededBindings);
        }

        return result;
    }

    private static IReadOnlyList<BindingRecord> LoadImportedBindings(
        FacilityEditorStateService editorStateService,
        string contentRootPath,
        ILogger logger)
    {
        try
        {
            var importedBindings = editorStateService.GetImportedBindingsSnapshot();

            return importedBindings
                .Select(binding => CreateImportedBindingRecord(binding, null, contentRootPath))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FacilityDataBindingRegistry: imported bindings overlay could not be loaded from editor state.");
            return [];
        }
    }

    private static BindingRecord CreateImportedBindingRecord(
        FacilityImportedBindingState state,
        string? absoluteFilePath,
        string contentRootPath)
    {
        var storageRelativePath = state.StorageRelativePath.Replace('/', Path.DirectorySeparatorChar);
        var relativeFolder = Path.GetDirectoryName(storageRelativePath)?.Replace('\\', '/') ?? string.Empty;
        var resolvedAbsolutePath = absoluteFilePath;

        if (string.IsNullOrWhiteSpace(resolvedAbsolutePath) && !string.IsNullOrWhiteSpace(state.StorageRelativePath))
        {
            resolvedAbsolutePath = Path.GetFullPath(Path.Combine(contentRootPath, state.StorageRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        return new BindingRecord
        {
            BindingId = state.BindingId,
            NodeId = state.NodeKey,
            MeterUrn = state.MeterUrn ?? string.Empty,
            MeterFolder = relativeFolder,
            FileName = Path.GetFileName(storageRelativePath),
            MeasurementKey = state.ExactSignalCode,
            DataStage = "imported",
            Resolution = state.Resolution,
            Category = FacilitySignalTaxonomy.ResolveFamily(state.ExactSignalCode).ToString(),
            Unit = state.Unit,
            SourceLabel = state.SourceLabel,
            OriginalFileName = state.OriginalFileName,
            SourceFilePath = resolvedAbsolutePath,
            ImportedUtc = state.ImportedUtc,
            UsesFixedCsvSeriesFormat = state.FileFormat == FacilityImportedBindingFileFormat.FixedCsvSeries,
        };
    }


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
                            BindingId      = $"seed::{r.NodeId}::{r.MeterUrn}::{r.MeasurementKey}::{r.FileName}",
                            NodeId         = r.NodeId,
                            MeterUrn       = r.MeterUrn,
                            MeterFolder    = r.MeterFolder,
                            FileName       = r.FileName,
                            MeasurementKey = r.MeasurementKey,
                            DataStage      = r.DataStage,
                            Resolution     = r.Resolution,
                            Category       = r.Category,
                            Unit           = string.Empty,
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
