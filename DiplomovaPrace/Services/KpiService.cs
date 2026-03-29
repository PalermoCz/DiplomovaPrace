namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models;
using DiplomovaPrace.Models.Kpi;

public class KpiService : IKpiService
{
    private readonly IMeasurementRepository _repository;
    private readonly IBuildingStateService _buildingState;

    public KpiService(IMeasurementRepository repository, IBuildingStateService buildingState)
    {
        _repository = repository;
        _buildingState = buildingState;
    }

    private Device? GetDevice(string deviceId)
    {
        var building = _buildingState.Building;
        if (building == null) return null;

        foreach (var floor in building.Floors)
        {
            foreach (var room in floor.Rooms)
            {
                var device = room.Devices.FirstOrDefault(d => d.Id == deviceId);
                if (device != null) return device;
            }
        }
        return null;
    }

    public async Task<MeterKpiResult> CalculateBasicKpiAsync(KpiQuery query, CancellationToken ct = default)
    {
        var device = GetDevice(query.DeviceId);
        if (device == null)
        {
            return new MeterKpiResult(query.DeviceId, 0, null, null, null, null, null, null, null, null, null, null, null, false, "Device neexistuje v konfiguraci budovy.");
        }

        if (!device.Type.IsMeteringDevice())
        {
            return new MeterKpiResult(query.DeviceId, 0, null, null, null, null, null, null, null, null, null, null, null, false, $"Device {query.DeviceId} není smart metering zařízení.");
        }

        var records = await _repository.GetRangeAsync(query.DeviceId, query.From, query.To, ct);
        if (records.Count == 0)
        {
            return new MeterKpiResult(query.DeviceId, 0, null, null, null, null, null, null, null, null, null, null, null, false);
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
                    
                    if (IsWorkingHour(curr.Timestamp))
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
                        if (IsWorkingHour(curr.Timestamp)) workingHoursConsumption += energyChunk;
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

                    if (IsWorkingHour(curr.Timestamp))
                        workingHoursConsumption += energyChunk;
                    else
                        offHoursConsumption += energyChunk;
                }
            }
        }

        return new MeterKpiResult(
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
            IsEstimatedConsumption: isEstimated
        );
    }

    public async Task<KpiComparisonResult> ComparePeriodsAsync(
        string deviceId, 
        DateTime fromA, DateTime toA, 
        DateTime fromB, DateTime toB, 
        CancellationToken ct = default)
    {
        var resultA = await CalculateBasicKpiAsync(new KpiQuery(deviceId, fromA, toA), ct);
        var resultB = await CalculateBasicKpiAsync(new KpiQuery(deviceId, fromB, toB), ct);

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

    private bool IsWorkingHour(DateTime timestamp)
    {
        var localTime = timestamp.ToLocalTime();
        if (localTime.DayOfWeek == DayOfWeek.Saturday || localTime.DayOfWeek == DayOfWeek.Sunday)
            return false;
            
        return localTime.Hour >= 8 && localTime.Hour < 18;
    }
}
