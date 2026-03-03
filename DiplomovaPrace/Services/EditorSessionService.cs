namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models.Configuration;

/// <summary>
/// Scoped implementace IEditorSessionService.
/// Jedna instance pro každou Blazor circuit (záložku prohlížeče).
/// Žádná thread-safety není potřeba — Blazor Server garantuje single-threaded přístup
/// ke Scoped službám v rámci circuit.
///
/// Undo/Redo: snapshot-based (ukládáme celou BuildingConfig před každou mutací).
/// Hloubka zásobníku: max 50 kroků.
/// </summary>
public class EditorSessionService : IEditorSessionService
{
    private const int MaxHistoryDepth = 50;

    // ── Přechodný stav ────────────────────────────────────────────────────

    public BuildingConfig? CurrentConfig { get; private set; }
    public string? ActiveFloorId { get; private set; }
    public string? SelectedElementId { get; private set; }
    public EditorSelectionType SelectedElementType { get; private set; } = EditorSelectionType.None;
    public EditorTool ActiveTool { get; private set; } = EditorTool.Select;
    public bool HasUnsavedChanges { get; private set; }
    public PublicationState PublicationState { get; private set; } = PublicationState.Draft;

    // ── Undo / Redo ───────────────────────────────────────────────────────

    private readonly List<(BuildingConfig Snapshot, string Description)> _undoStack = [];
    private readonly List<(BuildingConfig Snapshot, string Description)> _redoStack = [];

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public string? UndoDescription => CanUndo ? _undoStack[^1].Description : null;
    public string? RedoDescription => CanRedo ? _redoStack[^1].Description : null;

    public event Action? OnSessionChanged;

    // ── Konfigurace ───────────────────────────────────────────────────────

    public void LoadConfig(BuildingConfig config)
    {
        CurrentConfig = config;
        ActiveFloorId = config.Floors.FirstOrDefault(f => !f.IsDeleted)?.Id;
        SelectedElementId = null;
        SelectedElementType = EditorSelectionType.Building;
        ActiveTool = EditorTool.Select;
        HasUnsavedChanges = false;
        PublicationState = PublicationState.Draft;
        _undoStack.Clear();
        _redoStack.Clear();
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
        PublicationState = PublicationState.Draft;
        _undoStack.Clear();
        _redoStack.Clear();
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
            if (PublicationState == PublicationState.Published)
                PublicationState = PublicationState.Modified;
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

    public void MarkPublished()
    {
        HasUnsavedChanges = false;
        PublicationState = PublicationState.Published;
        Notify();
    }

    public void RefreshConfig(BuildingConfig updatedConfig)
    {
        CurrentConfig = updatedConfig;
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

    // ── Příkazy (Undo/Redo) ───────────────────────────────────────────────

    public async Task ExecuteCommandAsync(
        IEditorCommand command,
        Func<Task> action,
        IBuildingConfigurationService configService)
    {
        if (CurrentConfig is null) return;

        // Uložíme snapshot před akcí
        PushUndo(CurrentConfig, command.Description);
        _redoStack.Clear();

        // Provedeme mutaci
        await action();

        // Načteme aktualizovanou konfiguraci
        var updated = await configService.GetBuildingAsync(CurrentConfig.Id);
        if (updated is not null)
            RefreshConfig(updated);

        MarkDirty();
    }

    public async Task UndoAsync(IBuildingConfigurationService configService)
    {
        if (!CanUndo || CurrentConfig is null) return;

        var entry = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);

        // Aktuální stav uložíme do redo zásobníku
        _redoStack.Add((CurrentConfig, entry.Description));

        await configService.ReplaceConfigAsync(entry.Snapshot);
        RefreshConfig(entry.Snapshot);
        MarkDirty();
    }

    public async Task RedoAsync(IBuildingConfigurationService configService)
    {
        if (!CanRedo || CurrentConfig is null) return;

        var entry = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);

        _undoStack.Add((CurrentConfig, entry.Description));

        await configService.ReplaceConfigAsync(entry.Snapshot);
        RefreshConfig(entry.Snapshot);
        MarkDirty();
    }

    // ── Lookup helpers ────────────────────────────────────────────────────

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

    // ── Privátní helpers ──────────────────────────────────────────────────

    private void PushUndo(BuildingConfig snapshot, string description)
    {
        _undoStack.Add((snapshot, description));
        if (_undoStack.Count > MaxHistoryDepth)
            _undoStack.RemoveAt(0);
    }

    private void Notify() => OnSessionChanged?.Invoke();
}
