namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models;

/// <summary>
/// Čisté statické funkce mapující stav zařízení na SVG vizuální atributy.
/// Všechny metody jsou deterministické a bez vedlejších efektů.
/// </summary>
public static class StateColorMapper
{
    /// <summary>
    /// Mapuje teplotu na barvu gradientu: modrá (chladno) → zelená (komfort) → oranžová → červená (horko).
    /// </summary>
    public static string GetTemperatureColor(double celsius) => celsius switch
    {
        < 18.0 => "#3498db",
        < 22.0 => "#2ecc71",
        < 26.0 => "#f39c12",
        < 30.0 => "#e67e22",
        _      => "#e74c3c"
    };

    /// <summary>
    /// Mapuje stav zařízení na SVG fill barvu podle typu zařízení.
    /// Alarm vždy přepisuje ostatní barvy.
    /// </summary>
    public static string GetDeviceColor(DeviceType type, DeviceState? state)
    {
        if (state is null) return "#95a5a6";
        if (state.IsAlarm) return "#e74c3c";

        return type switch
        {
            DeviceType.TemperatureSensor => GetTemperatureColor(state.NumericValue ?? 20.0),
            DeviceType.HumiditySensor    => state.NumericValue > 70.0 ? "#e67e22" : "#3498db",
            DeviceType.Light             => state.IsActive == true ? "#f1c40f" : "#7f8c8d",
            DeviceType.HVAC              => state.IsActive == true ? "#2ecc71" : "#7f8c8d",
            DeviceType.MotionSensor      => state.IsActive == true ? "#9b59b6" : "#7f8c8d",
            DeviceType.DoorSensor        => state.IsActive == true ? "#e67e22" : "#2ecc71",
            // Smart metering — specifické barvy dle stavu měření
            DeviceType.EnergyMeter       => state.IsActive == true ? "#27ae60" : "#7f8c8d",
            DeviceType.PowerMeter        => state.IsActive == true ? "#2980b9" : "#7f8c8d",
            DeviceType.CurrentTransformer => state.IsActive == true ? "#16a085" : "#7f8c8d",
            _                            => "#95a5a6"
        };
    }

    /// <summary>
    /// Vypočítá agregovanou barvu výplně místnosti z jejích zařízení.
    /// Alarm má vždy přednost, jinak se použije teplota s transparencí.
    /// </summary>
    public static string GetRoomFillColor(
        IEnumerable<Device> devices,
        Func<string, DeviceState?> stateResolver)
    {
        var hasAlarm = false;
        double? temperature = null;

        foreach (var device in devices)
        {
            var state = stateResolver(device.Id);
            if (state is null) continue;
            if (state.IsAlarm) hasAlarm = true;
            if (device.Type == DeviceType.TemperatureSensor && state.NumericValue.HasValue)
                temperature = state.NumericValue.Value;
        }

        if (hasAlarm) return "#e74c3c33";

        if (temperature.HasValue)
            return GetTemperatureColor(temperature.Value) + "33";

        return "#ecf0f133";
    }
}
