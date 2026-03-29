namespace DiplomovaPrace.Models;

/// <summary>
/// Metadata měřicího bodu — připojitelná k libovolnému Device/DeviceConfig.
/// Nullable property: pokud zařízení není metering bod, metadata jsou null.
/// Navrženo pro budoucí EF Core persistenci (owned type).
/// </summary>
public record MeteringMetadata(
    /// <summary>Sériové číslo přístroje (např. z výrobního štítku).</summary>
    string? SerialNumber,

    /// <summary>Jmenovité napětí měřicího bodu (V). Např. 230 pro 1f, 400 pro 3f.</summary>
    double? NominalVoltageV,

    /// <summary>Jmenovitý proud (A). Např. 63 A pro hlavní jistič.</summary>
    double? NominalCurrentA,

    /// <summary>Převodový poměr CT (např. 100/5 = 20.0). Null = přímé měření.</summary>
    double? TransformationRatio,

    /// <summary>
    /// Kategorie zátěže / okruhu (volný text nebo konvence).
    /// Příklady: "Osvětlení", "HVAC", "Výroba", "IT", "Hlavní přívod".
    /// </summary>
    string? LoadCategory,

    /// <summary>
    /// Štítky pro kategorizaci a filtrování.
    /// Příklady: ["3f", "NT", "EM415"], ["1f", "VT"].
    /// </summary>
    IReadOnlyList<string>? Tags,

    /// <summary>Datum instalace měřidla.</summary>
    DateTime? InstallationDate,

    /// <summary>Interval měření v sekundách (pro import a simulaci). Null = default 2s.</summary>
    int? MeasurementIntervalSeconds
)
{
    /// <summary>Výchozí metadata pro nové metering zařízení.</summary>
    public static MeteringMetadata CreateDefault(DeviceType type) => type switch
    {
        DeviceType.EnergyMeter => new(
            SerialNumber: null,
            NominalVoltageV: 230.0,
            NominalCurrentA: 63.0,
            TransformationRatio: null,
            LoadCategory: null,
            Tags: null,
            InstallationDate: null,
            MeasurementIntervalSeconds: 900),  // 15 min = typický interval komerčních elektroměrů

        DeviceType.PowerMeter => new(
            SerialNumber: null,
            NominalVoltageV: 400.0,
            NominalCurrentA: 100.0,
            TransformationRatio: null,
            LoadCategory: null,
            Tags: null,
            InstallationDate: null,
            MeasurementIntervalSeconds: 60),  // 1 min = podrobnější monitoring

        DeviceType.CurrentTransformer => new(
            SerialNumber: null,
            NominalVoltageV: null,
            NominalCurrentA: 100.0,
            TransformationRatio: 20.0,  // 100/5
            LoadCategory: null,
            Tags: null,
            InstallationDate: null,
            MeasurementIntervalSeconds: 60),

        _ => new(null, null, null, null, null, null, null, null)
    };
}
