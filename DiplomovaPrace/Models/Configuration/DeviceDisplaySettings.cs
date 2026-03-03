using DiplomovaPrace.Models;

namespace DiplomovaPrace.Models.Configuration;

/// <summary>
/// Nastavení zobrazení zařízení v SVG. Odděleno od DeviceState (runtime hodnot).
/// EF Core mapuje jako owned type (OwnsOne).
/// </summary>
public record DeviceDisplaySettings(
    string? Unit,
    string? LabelOverride,
    double? AlarmThresholdHigh,
    double? AlarmThresholdLow,
    bool ShowValue
)
{
    public static DeviceDisplaySettings CreateDefault(DeviceType type) => type switch
    {
        DeviceType.TemperatureSensor => new("°C",  null, 30.0, 15.0, true),
        DeviceType.HumiditySensor    => new("%",   null, 80.0, 20.0, true),
        DeviceType.Light             => new(null,  null, null, null, false),
        DeviceType.HVAC              => new(null,  null, null, null, false),
        DeviceType.MotionSensor      => new(null,  null, null, null, false),
        DeviceType.DoorSensor        => new(null,  null, null, null, false),
        _                            => new(null,  null, null, null, false)
    };
}
