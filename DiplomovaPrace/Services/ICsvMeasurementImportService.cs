namespace DiplomovaPrace.Services;

using System.Globalization;
using DiplomovaPrace.Models;

public record CsvImportResult(
    int TotalLines,
    int ImportedCount,
    int SkippedCount,
    int ErrorCount,
    IReadOnlyList<CsvImportError> Errors,
    IReadOnlyList<string> UnknownDevices
);

public record CsvImportError(int LineNumber, string Reason);

public interface ICsvMeasurementImportService
{
    Task<CsvImportResult> ImportAsync(Stream csvStream, CancellationToken ct = default);
}
