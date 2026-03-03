namespace DiplomovaPrace.Models;

/// <summary>
/// Typy zařízení a senzorů v budově.
/// Rozšiřitelné přidáním nových členů bez změny ostatních vrstev.
/// </summary>
public enum DeviceType
{
    TemperatureSensor,
    HumiditySensor,
    Light,
    HVAC,
    MotionSensor,
    DoorSensor
}
