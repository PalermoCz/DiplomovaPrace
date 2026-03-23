namespace DiplomovaPrace.Services;

using System.Collections.Concurrent;
using DiplomovaPrace.Models;

/// <summary>
/// Singleton kontejner stavu budovy. Thread-safe díky ConcurrentDictionary.
/// Implementuje Observer pattern — komponenty se přihlašují k OnStateChanged eventu.
/// Inicializuje výchozí stavy všech zařízení při konstrukci, aby UI mělo data
/// ještě před prvním tikem simulace.
///
/// HISTORICKÁ DATA: Uchovává posledních 100 záznamů per zařízení v ring bufferu
/// pro time-series grafy. Thread-safe pomocí lock na individuální Queue.
/// </summary>
public class BuildingStateService : IBuildingStateService
{
    private readonly ConcurrentDictionary<string, DeviceState> _deviceStates = new();
    private readonly ConcurrentDictionary<string, Queue<StateRecord>> _deviceHistory = new();
    private const int MAX_HISTORY_SIZE = 100;

    private Building _building;
    public Building Building => _building;

    public event Action? OnStateChanged;

    public BuildingStateService()
    {
        _building = BuildingConfiguration.CreateDemoBuilding();
        InitializeDefaultStates();
    }

    public DeviceState? GetDeviceState(string deviceId)
    {
        _deviceStates.TryGetValue(deviceId, out var state);
        return state;
    }

    public IReadOnlyDictionary<string, DeviceState> AllDeviceStates => _deviceStates;

    public void UpdateDeviceState(string deviceId, DeviceState newState)
    {
        _deviceStates[deviceId] = newState;

        // Ukládání historie pouze pro zařízení s numeric value (sensors)
        if (newState.NumericValue.HasValue)
        {
            var queue = _deviceHistory.GetOrAdd(deviceId, _ => new Queue<StateRecord>());

            lock (queue)  // Queue není thread-safe, proto lock
            {
                queue.Enqueue(new StateRecord(newState.Timestamp, newState.NumericValue.Value));

                // Ring buffer: udržuj max MAX_HISTORY_SIZE záznamů
                while (queue.Count > MAX_HISTORY_SIZE)
                {
                    queue.Dequeue();
                }
            }
        }
    }

    public void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }

    public IReadOnlyList<StateRecord> GetDeviceHistory(string deviceId)
    {
        if (_deviceHistory.TryGetValue(deviceId, out var queue))
        {
            lock (queue)
            {
                return queue.ToList();  // Defensive copy pro thread-safety
            }
        }
        return Array.Empty<StateRecord>();
    }

    public void ClearHistory()
    {
        _deviceHistory.Clear();
    }

    public void ReplaceBuilding(Building newBuilding)
    {
        _building = newBuilding;
        _deviceStates.Clear();
        _deviceHistory.Clear();  // Vymazat historii při výměně budovy
        InitializeDefaultStates();
        NotifyStateChanged();
    }

    private void InitializeDefaultStates()
    {
        foreach (var floor in Building.Floors)
        foreach (var room in floor.Rooms)
        foreach (var device in room.Devices)
        {
            _deviceStates[device.Id] = DeviceState.CreateDefault(device.Type);
            // Inicializace prázdné history pro každé zařízení
            _deviceHistory.TryAdd(device.Id, new Queue<StateRecord>());
        }
    }
}
