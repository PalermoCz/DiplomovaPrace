namespace DiplomovaPrace.Services;

/// <summary>
/// Singleton implementace IActiveBuildingService.
/// Thread-safe — volatile read/write na _activeBuildingId.
/// Komponenty (NavMenu, BuildingViewer, EditorView) subscribeují k OnActiveBuildingChanged.
/// </summary>
public class ActiveBuildingService : IActiveBuildingService
{
    private volatile string? _activeBuildingId;

    public string? ActiveBuildingId => _activeBuildingId;

    public event Action? OnActiveBuildingChanged;

    public void SetActiveBuilding(string id)
    {
        if (_activeBuildingId == id) return;
        _activeBuildingId = id;
        OnActiveBuildingChanged?.Invoke();
    }
}
