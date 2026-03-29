namespace DiplomovaPrace.Models;

/// <summary>
/// Typy zařízení, senzorů a měřicích bodů.
/// Rozšiřitelné přidáním nových členů bez změny ostatních vrstev.
///
/// Skupiny:
///   - Building automation: TemperatureSensor .. DoorSensor
///   - Smart metering:      EnergyMeter .. CurrentTransformer
/// </summary>
public enum DeviceType
{
    // ── Building automation (stávající) ──────────────────────────────────
    TemperatureSensor,
    HumiditySensor,
    Light,
    HVAC,
    MotionSensor,
    DoorSensor,

    // ── Smart metering (nové) ────────────────────────────────────────────
    /// <summary>Elektroměr — měří kumulativní spotřebu energie (kWh).</summary>
    EnergyMeter,
    /// <summary>Analyzátor výkonu — měří okamžitý výkon (kW, kVA, kVAr), PF, V, A.</summary>
    PowerMeter,
    /// <summary>Proudový transformátor — měří proud (A) s převodem.</summary>
    CurrentTransformer
}

/// <summary>
/// Rozšiřující metody pro DeviceType — kategorizace typů.
/// </summary>
public static class DeviceTypeExtensions
{
    /// <summary>Vrací true pro typy patřící do smart metering domény.</summary>
    public static bool IsMeteringDevice(this DeviceType type) => type is
        DeviceType.EnergyMeter or
        DeviceType.PowerMeter or
        DeviceType.CurrentTransformer;

    /// <summary>Vrací true pro typy generující numerické hodnoty (senzory + metering).</summary>
    public static bool HasNumericValue(this DeviceType type) => type is
        DeviceType.TemperatureSensor or
        DeviceType.HumiditySensor or
        DeviceType.EnergyMeter or
        DeviceType.PowerMeter or
        DeviceType.CurrentTransformer;
}
