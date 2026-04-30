namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models;
using DiplomovaPrace.Models.Kpi;

public class KpiService : IKpiService
{
    private readonly IMeasurementRepository _repository;
    private readonly IActiveBuildingService _activeBuildingService;
    private readonly IBuildingConfigurationService _configService;

    public KpiService(
        IMeasurementRepository repository, 
        IActiveBuildingService activeBuildingService, 
        IBuildingConfigurationService configService)
    {
        _repository = repository;
        _activeBuildingService = activeBuildingService;
        _configService = configService;
    }

    private static void ReportProgress(
        IProgress<AnalyticsProgressUpdate>? progress,
        string stageKey,
        string phaseLabel,
        string detail,
        int completedSteps,
        int totalSteps)
    {
        progress?.Report(new AnalyticsProgressUpdate
        {
            StageKey = stageKey,
            PhaseLabel = phaseLabel,
            Detail = detail,
            CompletedSteps = completedSteps,
            TotalSteps = totalSteps
        });
    }

    private async Task<(DiplomovaPrace.Models.Configuration.DeviceConfig?, DiplomovaPrace.Models.Configuration.BuildingConfig?)> GetDeviceAndBuildingAsync(string deviceId)
    {
        var activeId = _activeBuildingService.ActiveBuildingId;
        if (string.IsNullOrEmpty(activeId)) return (null, null);

        var building = await _configService.GetBuildingAsync(activeId);
        if (building == null) return (null, null);

        foreach (var floor in building.Floors)
        {
            foreach (var room in floor.Rooms)
            {
                var device = room.Devices.FirstOrDefault(d => d.Id == deviceId);
                if (device != null) return (device, building);
            }
        }
        return (null, building);
    }

    public async Task<MeterKpiResult> CalculateBasicKpiAsync(
        KpiQuery query,
        CancellationToken ct = default,
        IProgress<AnalyticsProgressUpdate>? progress = null)
    {
        const int totalSteps = 4;

        var (device, buildingConfig) = await GetDeviceAndBuildingAsync(query.DeviceId);
        ReportProgress(progress, "kpi-resolve-scope", "Resolving KPI scope", "Resolved device metadata and building configuration for the requested KPI preview.", 1, totalSteps);

        if (device == null)
        {
            ReportProgress(progress, "kpi-finish", "Finalizing KPI preview", "KPI preview finished with a missing device configuration.", totalSteps, totalSteps);
            return new MeterKpiResult(query.DeviceId, 0, null, null, null, null, null, null, null, null, null, null, null, null, false, "Device neexistuje v konfiguraci budovy.");
        }

        if (!device.Type.IsMeteringDevice())
        {
            ReportProgress(progress, "kpi-finish", "Finalizing KPI preview", "KPI preview finished because the selected device is not a metering source.", totalSteps, totalSteps);
            return new MeterKpiResult(query.DeviceId, 0, null, null, null, null, null, null, null, null, null, null, null, null, false, $"Device {query.DeviceId} není smart metering zařízení.");
        }

        var records = await _repository.GetRangeAsync(query.DeviceId, query.From, query.To, ct);
        ReportProgress(progress, "kpi-load-measurements", "Loading KPI records", "Loaded the requested measurement range from the repository.", 2, totalSteps);

        if (records.Count == 0)
        {
            ReportProgress(progress, "kpi-compute", "Computing KPI statistics", "No measurements were available in the requested interval.", 3, totalSteps);
            ReportProgress(progress, "kpi-finish", "Finalizing KPI preview", "Built the final no-data KPI preview result.", totalSteps, totalSteps);
            return new MeterKpiResult(query.DeviceId, 0, null, null, null, null, null, null, null, null, null, null, null, null, false);
        }

        var first = records.First();
        var last = records.Last();
        var duration = last.Timestamp - first.Timestamp;

        double? peakPower = records.Max(r => r.ActivePowerKW);
        double? avgPower = records.Average(r => r.ActivePowerKW);
        double? avgVoltage = records.Average(r => r.VoltageV);
        double? avgCurrent = records.Average(r => r.CurrentA);
        double? avgPf = records.Average(r => r.PowerFactor);

        double totalConsumption = 0;
        double workingHoursConsumption = 0;
        double offHoursConsumption = 0;
        bool isEstimated = false;

        // Pokud máme kumulativní hodnoty, spočítáme jako rozdíl absolutních stavů.
        // Hledáme max a min ActiveEnergyKWh, předpokládáme že roste.
        var validEnergyRecords = records.Where(r => r.ActiveEnergyKWh.HasValue).ToList();
        
        if (validEnergyRecords.Count >= 2)
        {
            double candidateConsumption = validEnergyRecords.Last().ActiveEnergyKWh!.Value - validEnergyRecords.First().ActiveEnergyKWh!.Value;
            
            if (candidateConsumption >= 0)
            {
                // Kumulativní data jsou monotónní — používáme diferenční kumulativní výpočet.
                totalConsumption = candidateConsumption;
                
                for (int i = 1; i < validEnergyRecords.Count; i++)
                {
                    var prev = validEnergyRecords[i - 1];
                    var curr = validEnergyRecords[i];
                    var delta = curr.ActiveEnergyKWh!.Value - prev.ActiveEnergyKWh!.Value;
                    if (delta < 0) delta = 0; // ignorujeme reset čítače
                    
                    if (IsWorkingHour(curr.Timestamp, buildingConfig))
                        workingHoursConsumption += delta;
                    else
                        offHoursConsumption += delta;
                }
            }
            else
            {
                // Data nejsou monotónní (mix simulace + CSV). Fallback na integraci výkonu.
                isEstimated = true;
                for (int i = 1; i < records.Count; i++)
                {
                    var prev = records[i - 1];
                    var curr = records[i];
                    if (prev.ActivePowerKW.HasValue && curr.ActivePowerKW.HasValue)
                    {
                        var dtHours = (curr.Timestamp - prev.Timestamp).TotalHours;
                        var avgP = (prev.ActivePowerKW.Value + curr.ActivePowerKW.Value) / 2.0;
                        var energyChunk = avgP * dtHours;
                        totalConsumption += energyChunk;
                        if (IsWorkingHour(curr.Timestamp, buildingConfig)) workingHoursConsumption += energyChunk;
                        else offHoursConsumption += energyChunk;
                    }
                }
            }
        }
        else
        {
            // Nula nebo jeden kumulativní záznam — odhad z výkonu (ActivePowerKW) jako trapézová integrace.
            isEstimated = true;
            for (int i = 1; i < records.Count; i++)
            {
                var prev = records[i - 1];
                var curr = records[i];
                
                if (prev.ActivePowerKW.HasValue && curr.ActivePowerKW.HasValue)
                {
                    var dtHours = (curr.Timestamp - prev.Timestamp).TotalHours;
                    var avgP = (prev.ActivePowerKW.Value + curr.ActivePowerKW.Value) / 2.0;
                    var energyChunk = avgP * dtHours;
                    
                    totalConsumption += energyChunk;

                    if (IsWorkingHour(curr.Timestamp, buildingConfig))
                        workingHoursConsumption += energyChunk;
                    else
                        offHoursConsumption += energyChunk;
                }
            }
        }

        double? specificConsumption = null;
        var area = buildingConfig?.Metadata?.GrossFloorAreaM2;
        if (area.HasValue && area.Value > 0)
        {
            specificConsumption = totalConsumption / area.Value;
        }

        ReportProgress(progress, "kpi-compute", "Computing KPI statistics", "Computed KPI statistics from the loaded measurement range.", 3, totalSteps);

        var result = new MeterKpiResult(
            DeviceId: query.DeviceId,
            RecordCount: records.Count,
            FirstTimestamp: first.Timestamp,
            LastTimestamp: last.Timestamp,
            Duration: duration,
            PeakPowerKW: peakPower,
            AveragePowerKW: avgPower,
            AverageVoltageV: avgVoltage,
            AverageCurrentA: avgCurrent,
            AveragePowerFactor: avgPf,
            TotalConsumptionKWh: totalConsumption,
            WorkingHoursConsumptionKWh: workingHoursConsumption,
            OffHoursConsumptionKWh: offHoursConsumption,
            SpecificConsumptionKWhPerM2: specificConsumption,
            IsEstimatedConsumption: isEstimated
        );

        ReportProgress(progress, "kpi-finish", "Finalizing KPI preview", "Constructed the final KPI preview result.", totalSteps, totalSteps);
        return result;
    }

    public async Task<KpiComparisonResult> ComparePeriodsAsync(
        string deviceId, 
        DateTime fromA, DateTime toA, 
        DateTime fromB, DateTime toB, 
        CancellationToken ct = default,
        IProgress<AnalyticsProgressUpdate>? progress = null)
    {
        const int totalSteps = 8;

        IProgress<AnalyticsProgressUpdate>? CreateScopedProgress(string stagePrefix, int completedOffset, string detailPrefix)
        {
            if (progress is null)
            {
                return null;
            }

            return new Progress<AnalyticsProgressUpdate>(update =>
            {
                progress.Report(new AnalyticsProgressUpdate
                {
                    StageKey = $"{stagePrefix}-{update.StageKey}",
                    PhaseLabel = update.PhaseLabel,
                    Detail = $"{detailPrefix} {update.Detail}",
                    CompletedSteps = Math.Clamp(completedOffset + update.CompletedSteps, 0, totalSteps),
                    TotalSteps = totalSteps
                });
            });
        }

        var resultA = await CalculateBasicKpiAsync(
            new KpiQuery(deviceId, fromA, toA),
            ct,
            CreateScopedProgress("period-a", 0, "Primary period:"));
        var resultB = await CalculateBasicKpiAsync(
            new KpiQuery(deviceId, fromB, toB),
            ct,
            CreateScopedProgress("period-b", 4, "Comparison period:"));

        double? diffCons = (resultA.TotalConsumptionKWh.HasValue && resultB.TotalConsumptionKWh.HasValue) 
            ? resultA.TotalConsumptionKWh.Value - resultB.TotalConsumptionKWh.Value 
            : null;

        double? diffPeak = (resultA.PeakPowerKW.HasValue && resultB.PeakPowerKW.HasValue) 
            ? resultA.PeakPowerKW.Value - resultB.PeakPowerKW.Value 
            : null;

        double? diffAvg = (resultA.AveragePowerKW.HasValue && resultB.AveragePowerKW.HasValue) 
            ? resultA.AveragePowerKW.Value - resultB.AveragePowerKW.Value 
            : null;

        return new KpiComparisonResult(deviceId, resultA, resultB, diffCons, diffPeak, diffAvg);
    }

    private bool IsWorkingHour(DateTime timestamp, DiplomovaPrace.Models.Configuration.BuildingConfig? buildingConfig)
    {
        var meta = buildingConfig?.Metadata;
        var start = meta?.WorkingDayStart.Hours ?? 8;
        var end = meta?.WorkingDayEnd.Hours ?? 18;

        var localTime = timestamp.ToLocalTime();
        if (localTime.DayOfWeek == DayOfWeek.Saturday || localTime.DayOfWeek == DayOfWeek.Sunday)
            return false;
            
        return localTime.Hour >= start && localTime.Hour < end;
    }
}
