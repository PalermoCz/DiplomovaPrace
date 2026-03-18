namespace DiplomovaPrace.Models;

/// <summary>
/// Pozice ikony zařízení v SVG souřadnicovém systému půdorysu.
/// </summary>
public record DevicePosition(double X, double Y);

/// <summary>
/// Strukturální definice zařízení nebo senzoru v místnosti.
/// Stav zařízení je uložen odděleně v BuildingStateService — oddělení
/// statické konfigurace od dynamických dat zjednodušuje správu konkurence.
/// Consumption je statická vlastnost zařízení (příkon v Wattech).
/// </summary>
public record Device(
    string Id,
    string Name,
    DeviceType Type,
    string RoomId,
    DevicePosition Position,
    double Consumption = 0.0
)
{
    /// <summary>Výchozí příkon v Wattech dle typu zařízení.</summary>
    public static double DefaultConsumption(DeviceType type) => type switch
    {
        DeviceType.Light             => 100.0,
        DeviceType.HVAC              => 1500.0,
        DeviceType.TemperatureSensor => 5.0,
        DeviceType.HumiditySensor    => 5.0,
        DeviceType.MotionSensor      => 5.0,
        DeviceType.DoorSensor        => 3.0,
        _                            => 0.0
    };
}
