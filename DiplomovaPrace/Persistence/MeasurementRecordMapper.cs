using DiplomovaPrace.Models;

namespace DiplomovaPrace.Persistence;

/// <summary>
/// Mapování mezi doménovým MeasurementRecord (record, immutable)
/// a persistence entitou MeasurementRecordEntity (class, mutable).
///
/// Záměrné oddělení:
///   - doménový record = přenos dat between services, nezávisí na EF
///   - entita = záleží na EF Core, má Id, DataAnnotations
/// </summary>
public static class MeasurementRecordMapper
{
    /// <summary>Převod z doménového modelu → EF entita (pro Save).</summary>
    public static MeasurementRecordEntity ToEntity(MeasurementRecord domain) => new()
    {
        // Id není nastavováno — generuje SQLite automaticky
        DeviceId             = domain.DeviceId,
        Timestamp            = domain.Timestamp,
        ActiveEnergyKWh      = domain.ActiveEnergyKWh,
        ReactiveEnergyKVArh  = domain.ReactiveEnergyKVArh,
        ActivePowerKW        = domain.ActivePowerKW,
        ReactivePowerKVAr    = domain.ReactivePowerKVAr,
        ApparentPowerKVA     = domain.ApparentPowerKVA,
        VoltageV             = domain.VoltageV,
        CurrentA             = domain.CurrentA,
        PowerFactor          = domain.PowerFactor,
        FrequencyHz          = domain.FrequencyHz
    };

    /// <summary>Převod z EF entita → doménový model (pro Read / Query).</summary>
    public static MeasurementRecord ToDomain(MeasurementRecordEntity entity) => new(
        Timestamp:           entity.Timestamp,
        DeviceId:            entity.DeviceId,
        ActiveEnergyKWh:     entity.ActiveEnergyKWh,
        ReactiveEnergyKVArh: entity.ReactiveEnergyKVArh,
        ActivePowerKW:       entity.ActivePowerKW,
        ReactivePowerKVAr:   entity.ReactivePowerKVAr,
        ApparentPowerKVA:    entity.ApparentPowerKVA,
        VoltageV:            entity.VoltageV,
        CurrentA:            entity.CurrentA,
        PowerFactor:         entity.PowerFactor,
        FrequencyHz:         entity.FrequencyHz
    );
}
