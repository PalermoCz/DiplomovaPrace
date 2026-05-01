namespace DiplomovaPrace.Services;

using System.Globalization;
using DiplomovaPrace.Models;

public class CsvMeasurementImportService : ICsvMeasurementImportService
{
    private readonly IMeasurementRepository _repository;

    public CsvMeasurementImportService(IMeasurementRepository repository)
    {
        _repository = repository;
    }

    public async Task<CsvImportResult> ImportAsync(Stream csvStream, CancellationToken ct = default)
    {
        var errors = new List<CsvImportError>();
        var recordsToSave = new List<MeasurementRecord>();
        
        int totalLines = 0;
        int importedCount = 0;
        int skippedCount = 0;
        int errorCount = 0;

        using var reader = new StreamReader(csvStream);
        
        string? headerLine = await reader.ReadLineAsync(ct);
        if (headerLine == null)
        {
            return new CsvImportResult(0, 0, 0, 1, new[] { new CsvImportError(0, "CSV soubor je prázdný.") }, Array.Empty<string>());
        }

        var headers = headerLine.Split(new[] { ',', ';' }).Select(h => h.Trim()).ToList();
        
        int idxDeviceId = headers.IndexOf("DeviceId");
        int idxTimestamp = headers.IndexOf("Timestamp");
        int idxActiveEnergy = headers.IndexOf("ActiveEnergyKWh");
        int idxActivePower = headers.IndexOf("ActivePowerKW");
        int idxVoltage = headers.IndexOf("VoltageV");
        int idxCurrent = headers.IndexOf("CurrentA");
        int idxPowerFactor = headers.IndexOf("PowerFactor");

        if (idxDeviceId < 0 || idxTimestamp < 0)
        {
            return new CsvImportResult(1, 0, 0, 1, new[] { new CsvImportError(1, "Chybí povinné sloupce: DeviceId, Timestamp") }, Array.Empty<string>());
        }
        
        totalLines = 1;

        while (true)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;
            
            totalLines++;
            
            if (string.IsNullOrWhiteSpace(line))
            {
                skippedCount++;
                continue;
            }

            var parts = line.Split(new[] { ',', ';' });
            
            if (parts.Length <= idxDeviceId || parts.Length <= idxTimestamp)
            {
                errors.Add(new CsvImportError(totalLines, "Neplatný formát řádku - chybí sloupce."));
                errorCount++;
                continue;
            }

            string deviceId = parts[idxDeviceId].Trim();
            string timestampStr = parts[idxTimestamp].Trim();

            if (string.IsNullOrEmpty(deviceId))
            {
                errors.Add(new CsvImportError(totalLines, "DeviceId je prázdné."));
                errorCount++;
                continue;
            }

            if (!DateTime.TryParse(timestampStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp))
            {
                errors.Add(new CsvImportError(totalLines, $"Neplatný formát Timestamp: {timestampStr}"));
                errorCount++;
                continue;
            }
            
            double? ParseDouble(int index)
            {
                if (index < 0 || index >= parts.Length) return null;
                var val = parts[index].Trim();
                if (string.IsNullOrEmpty(val)) return null;
                
                val = val.Replace(',', '.');
                if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
                {
                    return parsed;
                }
                return null;
            }

            double? activeEnergy = ParseDouble(idxActiveEnergy);
            double? activePower = ParseDouble(idxActivePower);
            double? voltage = ParseDouble(idxVoltage);
            double? current = ParseDouble(idxCurrent);
            double? powerFactor = ParseDouble(idxPowerFactor);

            var record = new MeasurementRecord(
                Timestamp: timestamp.ToUniversalTime(),
                DeviceId: deviceId,
                ActiveEnergyKWh: activeEnergy,
                ReactiveEnergyKVArh: null,
                ActivePowerKW: activePower,
                ReactivePowerKVAr: null,
                ApparentPowerKVA: null,
                VoltageV: voltage,
                CurrentA: current,
                PowerFactor: powerFactor,
                FrequencyHz: null
            );

            recordsToSave.Add(record);
            importedCount++;

            if (recordsToSave.Count >= 1000)
            {
                await _repository.SaveBatchAsync(recordsToSave, ct);
                recordsToSave.Clear();
            }
        }

        if (recordsToSave.Count > 0)
        {
            await _repository.SaveBatchAsync(recordsToSave, ct);
        }

        return new CsvImportResult(
            TotalLines: totalLines,
            ImportedCount: importedCount,
            SkippedCount: skippedCount,
            ErrorCount: errorCount,
            Errors: errors,
            UnknownDevices: Array.Empty<string>()
        );
    }
}
