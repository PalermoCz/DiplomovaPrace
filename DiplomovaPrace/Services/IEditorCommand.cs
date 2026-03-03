namespace DiplomovaPrace.Services;

/// <summary>
/// Popisovač příkazu pro undo/redo historii editoru.
/// Neobsahuje logiku — funguje jako datový marker (descriptor pattern).
/// Snapshot celé BuildingConfig se ukládá do HistoryEntry v EditorSessionService.
/// </summary>
public interface IEditorCommand
{
    /// <summary>Lidsky čitelný popis akce, zobrazovaný v tlačítku Undo/Redo.</summary>
    string Description { get; }
}
