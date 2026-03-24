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

    private PeriodicTimer? _timer;
    private Task? _executingTask;
    private CancellationTokenSource? _cts;

    public bool IsRunning { get; private set; }
    public long CurrentTick { get; private set; }

    public SimulationService(
        IBuildingStateService stateService,
        ILogger<SimulationService> logger)
    {
        _stateService = stateService;
        _logger = logger;
        _random = new Random(42);
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

            _ => DeviceState.CreateDefault(device.Type)
        };
    }

    private static double Clamp(double value, double min, double max)
        => Math.Max(min, Math.Min(max, value));

    public void Dispose()
    {
        _cts?.Cancel();
        _timer?.Dispose();
        _cts?.Dispose();
    }
}
