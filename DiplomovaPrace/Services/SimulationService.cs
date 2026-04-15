namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models;

/// <summary>
/// Simulační engine běžící na pozadí jako IHostedService.
/// Generuje deterministické změny stavů zařízení pomocí seeded Random instance.
///
/// Klíčové vlastnosti:
/// - PeriodicTimer s intervalem 2 sekundy
/// - Deterministický seed (42) pro reprodukovatelné výsledky
/// - Random walk pro numerické hodnoty (plynulé přechody)
/// - Dávková notifikace — jeden event po aktualizaci všech zařízení
/// </summary>
public class SimulationService : IHostedService, ISimulationService, IDisposable
{
    private readonly IBuildingStateService _stateService;
    private readonly ILogger<SimulationService> _logger;
    private readonly Random _random;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(2);
    private readonly MeasurementPersistenceService? _persistence;

    private PeriodicTimer? _timer;
    private Task? _executingTask;
    private CancellationTokenSource? _cts;

    public bool IsRunning { get; private set; }
    public long CurrentTick { get; private set; }

    public SimulationService(
        IBuildingStateService stateService,
        ILogger<SimulationService> logger,
        MeasurementPersistenceService? persistence = null)
    {
        _stateService = stateService;
        _logger       = logger;
        _persistence  = persistence;
        _random       = new Random(42);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Simulace spuštěna: seed=42, interval={Interval}s", _interval.TotalSeconds);
        Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Simulace zastavena na tiku {Tick}", CurrentTick);
        Stop();
        return Task.CompletedTask;
    }

    public void Start()
    {
        if (IsRunning) return;
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(_interval);
        _executingTask = ExecuteAsync(_cts.Token);
        IsRunning = true;
    }

    public void Stop()
    {
        if (!IsRunning) return;
        _cts?.Cancel();
        _timer?.Dispose();
        IsRunning = false;
    }

    private async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(ct))
            {
                CurrentTick++;
                SimulateTick();
            }
        }
        catch (OperationCanceledException)
        {
            // Očekávané při zastavení
        }
    }

    private void SimulateTick()
    {
        var building = _stateService.Building;

        foreach (var floor in building.Floors)
        foreach (var room in floor.Rooms)
        foreach (var device in room.Devices)
        {
            var currentState = _stateService.GetDeviceState(device.Id);
            var newState = GenerateDeviceState(device, currentState);
            _stateService.UpdateDeviceState(device.Id, newState);

            // Pro metering zařízení generujeme i detailní MeasurementRecord
            if (device.Type.IsMeteringDevice())
            {
                var measurement = GenerateMeasurementRecord(device, newState);
                // 1) In-memory ring buffer (pro živé grafy)
                _stateService.AddMeasurement(device.Id, measurement);
                // 2) Persistentní DB (fire-and-forget přes channel)
                _persistence?.Enqueue(measurement);
            }
        }

        _stateService.NotifyStateChanged();
    }

    /// <summary>
    /// Generuje nový stav zařízení. Používá aktuální stav pro plynulé přechody
    /// (malé delty) místo náhodných skoků. Binární stavy se přepínají s nízkou
    /// pravděpodobností pro realistické chování.
    /// </summary>
    private DeviceState GenerateDeviceState(Device device, DeviceState? current)
    {
        var now = DateTime.Now;

        return device.Type switch
        {
            DeviceType.TemperatureSensor => new DeviceState(
                NumericValue: Clamp((current?.NumericValue ?? 20.0) + (_random.NextDouble() - 0.5) * 2.0, 15.0, 35.0),
                IsActive: true,
                IsAlarm: (current?.NumericValue ?? 20.0) > 30.0,
                Timestamp: now),

            DeviceType.HumiditySensor => new DeviceState(
                NumericValue: Clamp((current?.NumericValue ?? 45.0) + (_random.NextDouble() - 0.5) * 3.0, 20.0, 90.0),
                IsActive: true,
                IsAlarm: (current?.NumericValue ?? 45.0) > 75.0,
                Timestamp: now),

            DeviceType.Light => new DeviceState(
                NumericValue: null,
                IsActive: _random.NextDouble() > 0.1 ? (current?.IsActive ?? false) : !(current?.IsActive ?? false),
                IsAlarm: false,
                Timestamp: now),

            DeviceType.HVAC => new DeviceState(
                NumericValue: null,
                IsActive: _random.NextDouble() > 0.15 ? (current?.IsActive ?? false) : !(current?.IsActive ?? false),
                IsAlarm: false,
                Timestamp: now),

            DeviceType.MotionSensor => new DeviceState(
                NumericValue: null,
                IsActive: _random.NextDouble() > 0.7,
                IsAlarm: false,
                Timestamp: now),

            DeviceType.DoorSensor => new DeviceState(
                NumericValue: null,
                IsActive: _random.NextDouble() > 0.15 ? (current?.IsActive ?? false) : !(current?.IsActive ?? false),
                IsAlarm: false,
                Timestamp: now),

            // ── Smart metering ──────────────────────────────────────────────

            // EnergyMeter: NumericValue = kumulativní kWh (monotónně rostoucí)
            DeviceType.EnergyMeter => GenerateEnergyMeterState(current, now),

            // PowerMeter: NumericValue = okamžitý výkon v kW
            DeviceType.PowerMeter => GeneratePowerMeterState(current, now),

            // CurrentTransformer: NumericValue = okamžitý proud v A
            DeviceType.CurrentTransformer => GenerateCurrentTransformerState(current, now),

            _ => DeviceState.CreateDefault(device.Type)
        };
    }

    // ── Smart metering generátory ──────────────────────────────────────────

    /// <summary>
    /// EnergyMeter: kumulativní kWh roste o realistický přírůstek (base load + noise).
    /// Simuluje typický profil spotřeby budovy: ~5-50 kW base load, delta 2s.
    /// </summary>
    private DeviceState GenerateEnergyMeterState(DeviceState? current, DateTime now)
    {
        var currentKWh = current?.NumericValue ?? 0.0;
        // Base load 10-30 kW s náhodným kolísáním
        var powerKW = 15.0 + (_random.NextDouble() - 0.5) * 20.0;
        powerKW = Math.Max(2.0, powerKW); // Min 2 kW
        // kWh přírůstek za 2 sekundy = kW * (2/3600)
        var deltaKWh = powerKW * (_interval.TotalSeconds / 3600.0);
        return new DeviceState(
            NumericValue: Math.Round(currentKWh + deltaKWh, 3),
            IsActive: true,
            IsAlarm: false,
            Timestamp: now);
    }

    /// <summary>
    /// PowerMeter: okamžitý výkon osciluje kolem base load s random walk.
    /// </summary>
    private DeviceState GeneratePowerMeterState(DeviceState? current, DateTime now)
    {
        var currentKW = current?.NumericValue ?? 12.0;
        var delta = (_random.NextDouble() - 0.5) * 4.0;
        var newKW = Clamp(currentKW + delta, 1.0, 50.0);
        return new DeviceState(
            NumericValue: Math.Round(newKW, 2),
            IsActive: true,
            IsAlarm: newKW > 45.0, // Alarm při vysokém výkonu
            Timestamp: now);
    }

    /// <summary>
    /// CurrentTransformer: proud odvozený z typického výkonu na 230V.
    /// </summary>
    private DeviceState GenerateCurrentTransformerState(DeviceState? current, DateTime now)
    {
        var currentA = current?.NumericValue ?? 30.0;
        var delta = (_random.NextDouble() - 0.5) * 5.0;
        var newA = Clamp(currentA + delta, 0.5, 100.0);
        return new DeviceState(
            NumericValue: Math.Round(newA, 1),
            IsActive: true,
            IsAlarm: newA > 90.0, // Alarm při přetížení
            Timestamp: now);
    }

    /// <summary>
    /// Vytvoří detailní MeasurementRecord z aktuálního stavu metering zařízení.
    /// Doplňuje jednoduché NumericValue o kompletní elektrotechnické veličiny.
    /// </summary>
    private MeasurementRecord GenerateMeasurementRecord(Device device, DeviceState state)
    {
        var now = state.Timestamp;
        var baseVoltage = 230.0 + (_random.NextDouble() - 0.5) * 10.0;
        var pf = 0.85 + _random.NextDouble() * 0.13; // 0.85–0.98
        var freq = 50.0 + (_random.NextDouble() - 0.5) * 0.1;

        return device.Type switch
        {
            DeviceType.EnergyMeter => new MeasurementRecord(
                Timestamp: now,
                DeviceId: device.Id,
                ActiveEnergyKWh: state.NumericValue,
                ReactiveEnergyKVArh: (state.NumericValue ?? 0) * 0.3, // ~30 % jalové
                ActivePowerKW: 15.0 + (_random.NextDouble() - 0.5) * 10.0,
                ReactivePowerKVAr: 5.0 + (_random.NextDouble() - 0.5) * 3.0,
                ApparentPowerKVA: null, // dopočítá se z P a Q
                VoltageV: baseVoltage,
                CurrentA: (state.NumericValue ?? 10) > 0 ? 15.0 * 1000 / (baseVoltage * pf) : 0,
                PowerFactor: Math.Round(pf, 3),
                FrequencyHz: Math.Round(freq, 2)),

            DeviceType.PowerMeter => new MeasurementRecord(
                Timestamp: now,
                DeviceId: device.Id,
                ActiveEnergyKWh: null, // PowerMeter neměří kumulativní energii
                ReactiveEnergyKVArh: null,
                ActivePowerKW: state.NumericValue,
                ReactivePowerKVAr: (state.NumericValue ?? 0) * 0.35,
                ApparentPowerKVA: (state.NumericValue ?? 0) / pf,
                VoltageV: baseVoltage,
                CurrentA: (state.NumericValue ?? 0) * 1000 / (baseVoltage * pf),
                PowerFactor: Math.Round(pf, 3),
                FrequencyHz: Math.Round(freq, 2)),

            DeviceType.CurrentTransformer => new MeasurementRecord(
                Timestamp: now,
                DeviceId: device.Id,
                ActiveEnergyKWh: null,
                ReactiveEnergyKVArh: null,
                ActivePowerKW: null,
                ReactivePowerKVAr: null,
                ApparentPowerKVA: null,
                VoltageV: null, // CT neměří napětí
                CurrentA: state.NumericValue,
                PowerFactor: null,
                FrequencyHz: null),

            _ => new MeasurementRecord(now, device.Id, null, null, null, null, null, null, null, null, null)
        };
    }

    private static double Clamp(double value, double min, double max)
        => Math.Max(min, Math.Min(max, value));

    
private bool _disposed;

public void Dispose()
{
    if (_disposed)
    {
        return;
    }

    _disposed = true;

    var timer = _timer;
    _timer = null;
    timer?.Dispose();

    var cts = _cts;
    _cts = null;

    if (cts is not null)
    {
        try
        {
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
        }
        catch (ObjectDisposedException)
        {
            // už dispose-nutý
        }

        try
        {
            cts.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // už dispose-nutý
        }
    }
}

}

