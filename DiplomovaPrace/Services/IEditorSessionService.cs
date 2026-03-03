namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models.Configuration;

/// <summary>
/// Přechodný stav editoru pro jednu Blazor circuit (Scoped — per-záložka).
/// Drží: aktivní nástroj, vybraný prvek, aktuálně načtenou konfiguraci a příznak neuložených změn.
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

    // ── Observer ──────────────────────────────────────────────────────────────

    event Action? OnSessionChanged;

    // ── Konfigurace ───────────────────────────────────────────────────────────

    /// <summary>Načte konfiguraci do session a vybere první patro.</summary>
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

    // ── Synchronizace s IBuildingConfigurationService ─────────────────────────

    /// <summary>
    /// Aktualizuje CurrentConfig po mutaci v IBuildingConfigurationService.
    /// Volá se z EditorView po každém await ConfigService.*Async() volání.
    /// </summary>
    void RefreshConfig(BuildingConfig updatedConfig);

    // ── Lookup helpers ────────────────────────────────────────────────────────

    FloorConfig? GetActiveFloor();
    RoomConfig? FindRoom(string roomId);
    DeviceConfig? FindDevice(string deviceId);
}
