using System.Globalization;

namespace DiplomovaPrace.Services;

public sealed record FacilityNodeSeriesImportRequest(
    string NodeKey,
    string ExactSignalCode,
    string Unit,
    string OriginalFileName,
    string? MeterUrn,
    string? SourceLabel);

public sealed record FacilityNodeSeriesImportResult(
    bool Success,
    string Message,
    FacilityImportedBindingState? Binding,
    int ImportedCount,
    int SkippedCount,
    IReadOnlyList<CsvImportError> Errors);

public sealed record FacilityNodeBindingDeleteResult(
    bool Success,
    string Message,
    string? BindingId,
    string? ExactSignalCode);

public sealed class FacilityNodeSeriesImportService
{
    private readonly IWebHostEnvironment _environment;
    private readonly FacilityEditorStateService _editorStateService;
    private readonly FacilityDataBindingRegistry _bindingRegistry;
    private readonly ILogger<FacilityNodeSeriesImportService> _logger;

    public FacilityNodeSeriesImportService(
        IWebHostEnvironment environment,
        FacilityEditorStateService editorStateService,
        FacilityDataBindingRegistry bindingRegistry,
        ILogger<FacilityNodeSeriesImportService> logger)
    {
        _environment = environment;
        _editorStateService = editorStateService;
        _bindingRegistry = bindingRegistry;
        _logger = logger;
    }

    public async Task<FacilityNodeSeriesImportResult> ImportAsync(
        Stream csvStream,
        FacilityNodeSeriesImportRequest request,
        CancellationToken ct = default)
    {
        var normalizedRequest = NormalizeRequest(request);
        var exactSignalCode = FacilitySignalTaxonomy.NormalizeExactCode(normalizedRequest.ExactSignalCode);
        if (_bindingRegistry.GetBindings(normalizedRequest.NodeKey, exactSignalCode).Count > 0)
        {
            var duplicateMessage = $"Import blocked: node {normalizedRequest.NodeKey} already has a binding for exact signal code '{exactSignalCode.Value}'. Delete the existing binding first.";
            _logger.LogInformation(
                "FacilityNodeSeriesImportService: duplicate import blocked for node '{NodeKey}' and exact signal '{ExactSignalCode}'.",
                normalizedRequest.NodeKey,
                exactSignalCode.Value);

            return new FacilityNodeSeriesImportResult(false, duplicateMessage, null, 0, 0, []);
        }

        var errors = new List<CsvImportError>();
        var samples = new List<ImportedSeriesSample>();
        var lineNumber = 0;
        var skippedCount = 0;

        using var reader = new StreamReader(csvStream, leaveOpen: true);
        while (await reader.ReadLineAsync(ct) is { } rawLine)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                skippedCount++;
                continue;
            }

            var columns = rawLine.Split(',');
            if (lineNumber == 1 && LooksLikeHeader(columns))
            {
                continue;
            }

            if (columns.Length < 2)
            {
                errors.Add(new CsvImportError(lineNumber, "Expected CSV format: timestamp,value"));
                skippedCount++;
                continue;
            }

            if (!TryParseTimestamp(columns[0], out var timestamp))
            {
                errors.Add(new CsvImportError(lineNumber, $"Invalid timestamp in column 1: {columns[0].Trim()}"));
                skippedCount++;
                continue;
            }

            if (!TryParseValue(columns[1], out var value))
            {
                errors.Add(new CsvImportError(lineNumber, $"Invalid numeric value in column 2: {columns[1].Trim()}"));
                skippedCount++;
                continue;
            }

            samples.Add(new ImportedSeriesSample(timestamp, value));
        }

        if (samples.Count == 0)
        {
            var message = errors.Count == 0
                ? "Import failed: the CSV file does not contain any valid data rows."
                : "Import failed: no valid timestamp/value rows were found in the CSV file.";

            return new FacilityNodeSeriesImportResult(false, message, null, 0, skippedCount, errors);
        }

        var orderedSamples = samples
            .OrderBy(sample => sample.TimestampUtc)
            .ToList();
        var resolution = DetectResolution(orderedSamples);
        var bindingId = Guid.NewGuid().ToString("N");
        var relativePath = BuildStorageRelativePath(normalizedRequest.NodeKey, bindingId);
        var absolutePath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var absoluteDirectory = Path.GetDirectoryName(absolutePath);

        if (!string.IsNullOrWhiteSpace(absoluteDirectory))
        {
            Directory.CreateDirectory(absoluteDirectory);
        }

        try
        {
            await WriteNormalizedCsvAsync(absolutePath, orderedSamples, ct);

            var fileInfo = new FileInfo(absolutePath);
            var binding = new FacilityImportedBindingState
            {
                BindingId = bindingId,
                NodeKey = normalizedRequest.NodeKey,
                ExactSignalCode = normalizedRequest.ExactSignalCode,
                Unit = normalizedRequest.Unit,
                OriginalFileName = normalizedRequest.OriginalFileName,
                StorageRelativePath = relativePath,
                MeterUrn = normalizedRequest.MeterUrn,
                SourceLabel = normalizedRequest.SourceLabel,
                FileFormat = FacilityImportedBindingFileFormat.FixedCsvSeries,
                Resolution = resolution,
                FileSizeBytes = fileInfo.Exists ? fileInfo.Length : null,
                ImportedUtc = DateTime.UtcNow,
            };

            await _editorStateService.SaveImportedBindingAsync(binding, ct);
            _bindingRegistry.UpsertImportedBinding(binding, absolutePath);

            var message = errors.Count == 0
                ? $"Imported {orderedSamples.Count} rows for node {binding.NodeKey}."
                : $"Imported {orderedSamples.Count} rows for node {binding.NodeKey}; {errors.Count} invalid rows were skipped.";

            _logger.LogInformation(
                "FacilityNodeSeriesImportService: imported {Count} rows for node '{NodeKey}' into '{Path}' with exact signal '{ExactSignalCode}'.",
                orderedSamples.Count,
                binding.NodeKey,
                absolutePath,
                binding.ExactSignalCode);

            return new FacilityNodeSeriesImportResult(true, message, binding, orderedSamples.Count, skippedCount, errors);
        }
        catch
        {
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            throw;
        }
    }

    public async Task<FacilityNodeBindingDeleteResult> DeleteImportedBindingAsync(
        string? bindingId,
        CancellationToken ct = default)
    {
        var normalizedBindingId = bindingId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedBindingId))
        {
            return new FacilityNodeBindingDeleteResult(false, "Delete failed: binding id is missing.", null, null);
        }

        var deletedBinding = await _editorStateService.DeleteImportedBindingAsync(normalizedBindingId, ct);
        if (deletedBinding is null)
        {
            return new FacilityNodeBindingDeleteResult(false, "Delete failed: the imported binding was not found.", null, null);
        }

        _bindingRegistry.RemoveImportedBinding(deletedBinding.BindingId);
        TryDeleteImportedFile(deletedBinding.StorageRelativePath);

        _logger.LogInformation(
            "FacilityNodeSeriesImportService: deleted imported binding '{BindingId}' for node '{NodeKey}' and exact signal '{ExactSignalCode}'.",
            deletedBinding.BindingId,
            deletedBinding.NodeKey,
            deletedBinding.ExactSignalCode);

        return new FacilityNodeBindingDeleteResult(
            true,
            $"Binding '{deletedBinding.ExactSignalCode}' was deleted from node {deletedBinding.NodeKey}. You can import this exact signal code again.",
            deletedBinding.BindingId,
            deletedBinding.ExactSignalCode);
    }

    private static FacilityNodeSeriesImportRequest NormalizeRequest(FacilityNodeSeriesImportRequest request)
    {
        var nodeKey = request.NodeKey?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nodeKey))
        {
            throw new InvalidOperationException("Node key is required for import.");
        }

        if (!FacilitySignalTaxonomy.TryParseExactCode(request.ExactSignalCode, out var exactSignalCode) || exactSignalCode.IsEmpty)
        {
            throw new InvalidOperationException("Exact signal code is required for import.");
        }

        var unit = request.Unit?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new InvalidOperationException("Unit is required for import.");
        }

        var originalFileName = Path.GetFileName(request.OriginalFileName?.Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new InvalidOperationException("A source file name is required for import.");
        }

        return request with
        {
            NodeKey = nodeKey,
            ExactSignalCode = exactSignalCode.Value,
            Unit = unit,
            OriginalFileName = originalFileName,
            MeterUrn = NormalizeOptionalText(request.MeterUrn),
            SourceLabel = NormalizeOptionalText(request.SourceLabel)
        };
    }

    private static string BuildStorageRelativePath(string nodeKey, string bindingId)
    {
        return $"App_Data/facility-imports/{SanitizePathSegment(nodeKey)}/{bindingId}.csv";
    }

    private void TryDeleteImportedFile(string storageRelativePath)
    {
        if (string.IsNullOrWhiteSpace(storageRelativePath))
        {
            return;
        }

        var absolutePath = Path.GetFullPath(Path.Combine(
            _environment.ContentRootPath,
            storageRelativePath.Replace('/', Path.DirectorySeparatorChar)));

        if (!File.Exists(absolutePath))
        {
            return;
        }

        try
        {
            File.Delete(absolutePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FacilityNodeSeriesImportService: failed to delete imported file '{Path}'.", absolutePath);
        }
    }

    private static string SanitizePathSegment(string raw)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
        var sanitized = new string(raw
            .Trim()
            .Select(ch => invalidCharacters.Contains(ch) ? '_' : ch)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "node" : sanitized;
    }

    private static async Task WriteNormalizedCsvAsync(
        string absolutePath,
        IReadOnlyList<ImportedSeriesSample> samples,
        CancellationToken ct)
    {
        await using var stream = File.Create(absolutePath);
        await using var writer = new StreamWriter(stream);

        await writer.WriteLineAsync("timestamp,value");
        foreach (var sample in samples)
        {
            ct.ThrowIfCancellationRequested();
            var line = string.Create(
                CultureInfo.InvariantCulture,
                $"{sample.TimestampUtc:O},{sample.Value:G17}");
            await writer.WriteLineAsync(line);
        }

        await writer.FlushAsync(ct);
    }

    private static bool LooksLikeHeader(string[] columns)
    {
        if (columns.Length < 2)
        {
            return false;
        }

        var first = columns[0].Trim();
        var second = columns[1].Trim();

        return (first.Equals("timestamp", StringComparison.OrdinalIgnoreCase)
                || first.Equals("datetime_utc", StringComparison.OrdinalIgnoreCase)
                || first.Equals("time", StringComparison.OrdinalIgnoreCase))
            && second.Equals("value", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseTimestamp(string raw, out DateTime timestampUtc)
    {
        var value = raw.Trim();
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
        {
            timestampUtc = dto.UtcDateTime;
            return true;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
        {
            timestampUtc = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return true;
        }

        timestampUtc = default;
        return false;
    }

    private static bool TryParseValue(string raw, out double value)
    {
        return double.TryParse(raw.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)
            && double.IsFinite(value);
    }

    private static string DetectResolution(IReadOnlyList<ImportedSeriesSample> samples)
    {
        if (samples.Count < 2)
        {
            return "irregular";
        }

        TimeSpan? baseline = null;
        for (var index = 1; index < samples.Count; index++)
        {
            var delta = samples[index].TimestampUtc - samples[index - 1].TimestampUtc;
            if (delta <= TimeSpan.Zero)
            {
                continue;
            }

            if (!baseline.HasValue)
            {
                baseline = delta;
                continue;
            }

            if (delta != baseline.Value)
            {
                return "irregular";
            }
        }

        if (!baseline.HasValue)
        {
            return "irregular";
        }

        if (baseline.Value.TotalMinutes >= 1 && baseline.Value.TotalMinutes == Math.Round(baseline.Value.TotalMinutes))
        {
            return $"{baseline.Value.TotalMinutes:0}min";
        }

        return baseline.Value.TotalSeconds == Math.Round(baseline.Value.TotalSeconds)
            ? $"{baseline.Value.TotalSeconds:0}s"
            : "irregular";
    }

    private static string? NormalizeOptionalText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Trim();
    }

    private sealed record ImportedSeriesSample(DateTime TimestampUtc, double Value);
}