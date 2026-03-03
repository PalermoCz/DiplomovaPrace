namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models.Configuration;

/// <summary>
/// Scoped implementace IEditorSessionService.
/// Jedna instance pro každou Blazor circuit (záložku prohlížeče).
/// Žádná thread-safety není potřeba — Blazor Server garantuje single-threaded přístup
/// ke Scoped službám v rámci circuit.
/// </summary>
public class EditorSessionService : IEditorSessionService
{
    public BuildingConfig? CurrentConfig { get; private set; }
    public string? ActiveFloorId { get; private set; }
    public string? SelectedElementId { get; private set; }
    public EditorSelectionType SelectedElementType { get; private set; } = EditorSelectionType.None;
    public EditorTool ActiveTool { get; private set; } = EditorTool.Select;
    public bool HasUnsavedChanges { get; private set; }

    public event Action? OnSessionChanged;

    public void LoadConfig(BuildingConfig config)
    {
        CurrentConfig = config;
        ActiveFloorId = config.Floors.FirstOrDefault(f => !f.IsDeleted)?.Id;
        SelectedElementId = null;
        SelectedElementType = EditorSelectionType.Building;
        ActiveTool = EditorTool.Select;
        HasUnsavedChanges = false;
        Notify();
    }

    public void NewConfig()
    {
        CurrentConfig = null;
        ActiveFloorId = null;
        SelectedElementId = null;
        SelectedElementType = EditorSelectionType.None;
        ActiveTool = EditorTool.Select;
        HasUnsavedChanges = false;
        Notify();
    }

    public void SelectFloor(string floorId)
    {
        ActiveFloorId = floorId;
        ClearSelection();
    }

    public void SelectElement(string elementId, EditorSelectionType type)
    {
        SelectedElementId = elementId;
        SelectedElementType = type;
        Notify();
    }

    public void ClearSelection()
    {
        SelectedElementId = null;
        SelectedElementType = EditorSelectionType.None;
        Notify();
    }

    public void SetActiveTool(EditorTool tool)
    {
        ActiveTool = tool;
        // Změna nástroje ruší výběr prvku
        if (tool != EditorTool.Select)
        {
            SelectedElementId = null;
            SelectedElementType = EditorSelectionType.None;
        }
        Notify();
    }

    public void MarkDirty()
    {
        if (!HasUnsavedChanges)
        {
            HasUnsavedChanges = true;
            Notify();
        }
    }

    public void MarkClean()
    {
        if (HasUnsavedChanges)
        {
            HasUnsavedChanges = false;
            Notify();
        }
    }

    public void RefreshConfig(BuildingConfig updatedConfig)
    {
        CurrentConfig = updatedConfig;
        // Pokud aktivní patro bylo smazáno, přesměruj na první dostupné
        if (ActiveFloorId is not null &&
            !updatedConfig.Floors.Any(f => f.Id == ActiveFloorId && !f.IsDeleted))
        {
            ActiveFloorId = updatedConfig.Floors.FirstOrDefault(f => !f.IsDeleted)?.Id;
            ClearSelection();
        }
        else
        {
            Notify();
        }
    }

    public FloorConfig? GetActiveFloor() =>
        ActiveFloorId is null ? null
        : CurrentConfig?.Floors.FirstOrDefault(f => f.Id == ActiveFloorId && !f.IsDeleted);

    public RoomConfig? FindRoom(string roomId)
    {
        if (CurrentConfig is null) return null;
        foreach (var floor in CurrentConfig.Floors)
        foreach (var room in floor.Rooms)
            if (room.Id == roomId && !room.IsDeleted)
                return room;
        return null;
    }

    public DeviceConfig? FindDevice(string deviceId)
    {
        if (CurrentConfig is null) return null;
        foreach (var floor in CurrentConfig.Floors)
        foreach (var room in floor.Rooms)
        foreach (var device in room.Devices)
            if (device.Id == deviceId && !device.IsDeleted)
                return device;
        return null;
    }

    private void Notify() => OnSessionChanged?.Invoke();
}
