namespace DiplomovaPrace.Models;

/// <summary>
/// Immutabilní snapshot stavu zařízení v daném čase.
/// Record zajišťuje hodnotovou rovnost a thread-safe sdílení mezi vlákny.
/// Nullable pole reflektují, že ne všechny typy zařízení využívají všechny atributy.
/// </summary>
public record DeviceState(
    double? NumericValue,
    bool? IsActive,
    bool IsAlarm,
    DateTime Timestamp
)
{
    /// <summary>
    /// Tovární metoda pro vytvoření výchozího stavu podle typu zařízení.
    /// Volána při inicializaci BuildingStateService, aby každé zařízení
    /// mělo platný stav ještě před prvním tikem simulace.
    /// </summary>
    public static DeviceState CreateDefault(DeviceType type) => type switch
    {
        DeviceType.TemperatureSensor => new(20.0, true, false, DateTime.UtcNow),
        DeviceType.HumiditySensor   => new(45.0, true, false, DateTime.UtcNow),
        DeviceType.Light            => new(null, false, false, DateTime.UtcNow),
        DeviceType.HVAC             => new(null, false, false, DateTime.UtcNow),
        DeviceType.MotionSensor     => new(null, false, false, DateTime.UtcNow),
        DeviceType.DoorSensor       => new(null, false, false, DateTime.UtcNow),
        _                           => new(null, false, false, DateTime.UtcNow)
    };
}
