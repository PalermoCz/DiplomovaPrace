using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DiplomovaPrace.Models;
using DiplomovaPrace.Models.Configuration;

namespace DiplomovaPrace.Services;

/// <summary>
/// Importní služba (adaptér) pro integraci dat z The Building Data Genome 2 (BDG2).
/// Momentálně navrženo pro electricity-first využití nad vytvořeným subsetem.
/// </summary>
public class Bdg2ImportService
{
    private readonly IBuildingConfigurationService _configService;
    private readonly IMeasurementRepository _measurementRepository;
    private readonly ILogger<Bdg2ImportService> _logger;

    public Bdg2ImportService(
        IBuildingConfigurationService configService,
        IMeasurementRepository measurementRepository,
        ILogger<Bdg2ImportService> logger)
    {
        _configService = configService;
        _measurementRepository = measurementRepository;
        _logger = logger;
    }

    /// <summary>
    /// Načte metadata budov a electricity data.
    /// Vytvoří konfigurace budov a provede batch insert měření.
    /// </summary>
    public async Task ImportSubsetAsync(string metadataPath, string electricityPath, bool skipMeasurements = false)
    {
        _logger.LogInformation("Zahajuji import BDG2 dat...");
        
        // 1. Načtení metadat
        var buildings = new List<Bdg2MetadataRow>();
        using (var reader = new StreamReader(metadataPath))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }))
        {
            buildings = csv.GetRecords<Bdg2MetadataRow>().ToList();
        }

        var configuredDevices = new Dictionary<string, string>(); // building_id -> device_id
        var now = DateTime.UtcNow;

        // 2. Vytvoření konfigurace pro každou budovu se stabilními identifikátory
        foreach (var b in buildings)
        {
            var meta = new FacilityMetadata(
                OrganizationName: "BDG2 Dataset",
                SiteName: b.site_id,
                GrossFloorAreaM2: double.TryParse(b.sqm, NumberStyles.Any, CultureInfo.InvariantCulture, out var sqm) ? sqm : null,
                WorkingDayStart: TimeSpan.FromHours(8),
                WorkingDayEnd: TimeSpan.FromHours(18)
            );

            var buildingId = $"bdg2:{b.building_id}";
            var floorId = $"{buildingId}:floor0";
            var roomId = $"{buildingId}:room0";
            var deviceId = $"{buildingId}:em1";

            var deviceConfig = new DeviceConfig(
                Id: deviceId,
                RoomId: roomId,
                Name: "Main Electricity Meter",
                Type: DeviceType.EnergyMeter,
                Position: new DevicePosition(25, 25),
                DisplaySettings: DeviceDisplaySettings.CreateDefault(DeviceType.EnergyMeter),
                Consumption: 2.0,
                DisplayRules: Array.Empty<DisplayRule>(),
                CreatedAt: now,
                UpdatedAt: now,
                IsDeleted: false,
                MeteringMetadata: MeteringMetadata.CreateDefault(DeviceType.EnergyMeter)
            );

            var roomConfig = new RoomConfig(
                Id: roomId,
                FloorId: floorId,
                Name: "Hlavní rozvodna",
                Geometry: new RoomGeometry(0, 0, 50, 50),
                FillColorOverride: null,
                DisplayRules: Array.Empty<DisplayRule>(),
                Devices: new[] { deviceConfig },
                CreatedAt: now,
                UpdatedAt: now,
                IsDeleted: false
            );

            var floorConfig = new FloorConfig(
                Id: floorId,
                BuildingId: buildingId,
                Name: "Přízemí",
                Level: 0,
                Description: null,
                ViewBoxWidth: 800,
                ViewBoxHeight: 300,
                Rooms: new[] { roomConfig },
                CreatedAt: now,
                UpdatedAt: now,
                IsDeleted: false
            );

            var bConfig = new BuildingConfig(
                Id: buildingId,
                Name: b.building_id,
                Description: b.primaryspaceusage,
                Address: null,
                Metadata: meta,
                Floors: new[] { floorConfig },
                CreatedAt: now,
                UpdatedAt: now,
                CreatedBy: "import",
                UpdatedBy: "import",
                RowVersion: Array.Empty<byte>(),
                IsDeleted: false
            );

            await _configService.ReplaceConfigAsync(bConfig);

            configuredDevices[b.building_id] = deviceId;
            _logger.LogInformation("Založena budova {Name} s měřidlem {Device}", b.building_id, deviceId);
        }

        if (skipMeasurements)
        {
            _logger.LogInformation("Přeskočen import časových řad.");
            return;
        }

        // 3. Načtení electricity dat a průběžný parsing (wide format)
        using var readerElect = new StreamReader(electricityPath);
        using var csvElect = new CsvReader(readerElect, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });
        
        csvElect.Read();
        csvElect.ReadHeader();
        var headers = csvElect.HeaderRecord;
        if (headers == null)
            throw new InvalidOperationException("CSV neobsahuje hlavičku.");

        var runningSums = new Dictionary<string, double>();
        foreach (var h in headers.Skip(1)) runningSums[h] = 0.0;

        var batch = new List<MeasurementRecord>();
        int rowsProcessed = 0;

        while (csvElect.Read())
        {
            if (!DateTime.TryParse(csvElect.GetField(0), out var timestamp)) continue;
            
            // BDG2 jsou často UTC, převedeme na lokální nebo ponecháme
            // V app držíme DateTime jako Utc / Local agnostic, pro začátek ho necháme tak.

            for (int i = 1; i < headers.Length; i++)
            {
                var buildingId = headers[i];
                if (!configuredDevices.TryGetValue(buildingId, out var deviceId)) continue;
                
                var valStr = csvElect.GetField(i);
                if (string.IsNullOrWhiteSpace(valStr)) continue;
                if (!double.TryParse(valStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var val)) continue;

                // BDG2 spotřeba (hourly kWh). App potřebuje kumulativní (ActiveEnergyKWh) 
                // a ideálně i okamžitý (ActivePowerKW pro dashboard průběh).
                runningSums[buildingId] += val;

                var record = new MeasurementRecord(
                    Timestamp: timestamp,
                    DeviceId: deviceId,
                    ActiveEnergyKWh: runningSums[buildingId],
                    ReactiveEnergyKVArh: null,
                    ActivePowerKW: val, // průměrný výkon za tu hodinu (protože 1kWh za 1h = 1kW)
                    ReactivePowerKVAr: null,
                    ApparentPowerKVA: null,
                    VoltageV: null,
                    CurrentA: null,
                    PowerFactor: null,
                    FrequencyHz: null
                );
                batch.Add(record);
            }

            rowsProcessed++;
            if (batch.Count >= 5000)
            {
                await _measurementRepository.SaveBatchAsync(batch);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await _measurementRepository.SaveBatchAsync(batch);
        }

        _logger.LogInformation("Import BDG2 dat dokončen. Zpracováno časových řad: {RowsProcessed}", rowsProcessed);
    }

    private class Bdg2MetadataRow
    {
        public string building_id { get; set; } = string.Empty;
        public string site_id { get; set; } = string.Empty;
        public string primaryspaceusage { get; set; } = string.Empty;
        public string sqm { get; set; } = string.Empty;
    }
}
