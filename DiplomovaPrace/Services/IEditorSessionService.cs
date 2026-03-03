namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models.Configuration;

/// <summary>
/// Přechodný stav editoru pro jednu Blazor circuit (Scoped — per-záložka).
/// Drží: aktivní nástroj, vybraný prvek, aktuálně načtenou konfiguraci,
/// undo/redo zásobník (snapshot-based, max 50 kroků) a stav publikace.
///
/// Neukládá do perzistentního úložiště — pouze koordinuje stav v rámci jedné editor session.
/// Komponenty se přihlašují k OnSessionChanged a volají InvokeAsync(StateHasChanged).
/// </summary>
public interface IEditorSessionService
{
    // ── Stav ─────────────────────────────────────────────────────────────────

    BuildingConfig? CurrentConfig { get; }
    string? ActiveFloorId { get; }
    string? SelectedElementId { get; }
    EditorSelectionType SelectedElementType { get; }
    EditorTool ActiveTool { get; }
    bool HasUnsavedChanges { get; }

    // ── Undo / Redo ───────────────────────────────────────────────────────────

    bool CanUndo { get; }
    bool CanRedo { get; }

    /// <summary>Popis akce, která by se undoila (pro tooltip tlačítka). Null pokud nelze.</summary>
    string? UndoDescription { get; }

    /// <summary>Popis akce, která by se redoila. Null pokud nelze.</summary>
    string? RedoDescription { get; }

    // ── Stav publikace ────────────────────────────────────────────────────────

    PublicationState PublicationState { get; }

    // ── Observer ──────────────────────────────────────────────────────────────

    event Action? OnSessionChanged;

    // ── Konfigurace ───────────────────────────────────────────────────────────

    /// <summary>Načte konfiguraci do session a vybere první patro. Vymaže undo historii.</summary>
    void LoadConfig(BuildingConfig config);

    /// <summary>Resetuje session na prázdný stav (nová budova).</summary>
    void NewConfig();

    // ── Navigace ──────────────────────────────────────────────────────────────

    void SelectFloor(string floorId);

    // ── Výběr prvku ───────────────────────────────────────────────────────────

    void SelectElement(string elementId, EditorSelectionType type);
    void ClearSelection();

    // ── Nástroj ───────────────────────────────────────────────────────────────

    void SetActiveTool(EditorTool tool);

    // ── Dirty flag ────────────────────────────────────────────────────────────

    void MarkDirty();
    void MarkClean();

    /// <summary>Označí konfiguraci jako publikovanou.</summary>
    void MarkPublished();

    // ── Synchronizace s IBuildingConfigurationService ─────────────────────────

    /// <summary>
    /// Aktualizuje CurrentConfig po mutaci v IBuildingConfigurationService.
    /// Volá se z EditorView po každém await ConfigService.*Async() volání.
    /// </summary>
    void RefreshConfig(BuildingConfig updatedConfig);

    // ── Příkazy (Undo/Redo) ───────────────────────────────────────────────────

    /// <summary>
    /// Uloží snapshot před provedením akce, provede akci a notifikuje.
    /// Vzor: await SessionService.ExecuteCommandAsync(cmd, () => ConfigService.AddRoomAsync(...));
    /// </summary>
    Task ExecuteCommandAsync(IEditorCommand command, Func<Task> action,
        IBuildingConfigurationService configService);

    /// <summary>Vrátí o krok zpět. Noop pokud CanUndo == false.</summary>
    Task UndoAsync(IBuildingConfigurationService configService);

    /// <summary>Zopakuje krok vpřed. Noop pokud CanRedo == false.</summary>
    Task RedoAsync(IBuildingConfigurationService configService);

    // ── Lookup helpers ────────────────────────────────────────────────────────

    FloorConfig? GetActiveFloor();
    RoomConfig? FindRoom(string roomId);
    DeviceConfig? FindDevice(string deviceId);
}
