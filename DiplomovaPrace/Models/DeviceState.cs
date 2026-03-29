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
        DeviceType.TemperatureSensor => new(20.0, true, false, DateTime.Now),
        DeviceType.HumiditySensor   => new(45.0, true, false, DateTime.Now),
        DeviceType.Light            => new(null, false, false, DateTime.Now),
        DeviceType.HVAC             => new(null, false, false, DateTime.Now),
        DeviceType.MotionSensor     => new(null, false, false, DateTime.Now),
        DeviceType.DoorSensor       => new(null, false, false, DateTime.Now),
        // Smart metering — NumericValue = primární zobrazovaná hodnota
        DeviceType.EnergyMeter       => new(0.0, true, false, DateTime.Now),    // kWh kumulativní
        DeviceType.PowerMeter        => new(0.0, true, false, DateTime.Now),    // kW okamžitý
        DeviceType.CurrentTransformer => new(0.0, true, false, DateTime.Now),   // A okamžitý
        _                           => new(null, false, false, DateTime.Now)
    };
}
