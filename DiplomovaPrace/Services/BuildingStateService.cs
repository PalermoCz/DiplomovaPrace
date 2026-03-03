namespace DiplomovaPrace.Services;

using System.Collections.Concurrent;
using DiplomovaPrace.Models;

/// <summary>
/// Singleton kontejner stavu budovy. Thread-safe díky ConcurrentDictionary.
/// Implementuje Observer pattern — komponenty se přihlašují k OnStateChanged eventu.
/// Inicializuje výchozí stavy všech zařízení při konstrukci, aby UI mělo data
/// ještě před prvním tikem simulace.
/// </summary>
public class BuildingStateService : IBuildingStateService
{
    private readonly ConcurrentDictionary<string, DeviceState> _deviceStates = new();

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
    }

    public void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }

    public void ReplaceBuilding(Building newBuilding)
    {
        _building = newBuilding;
        _deviceStates.Clear();
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
        }
    }
}
