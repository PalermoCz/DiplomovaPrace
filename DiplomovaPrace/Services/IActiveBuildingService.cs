namespace DiplomovaPrace.Services;

/// <summary>
/// Sleduje globálně aktivní budovu (sdíleno mezi /building a /editor).
/// Singleton — jedna instance pro celou aplikaci.
/// Komponenty subscribeují k OnActiveBuildingChanged a reagují na přepnutí budovy.
/// </summary>
public interface IActiveBuildingService
{
    /// <summary>ID aktuálně aktivní budovy. Null před první inicializací.</summary>
    string? ActiveBuildingId { get; }

    /// <summary>Vyvoláno po každém přepnutí aktivní budovy.</summary>
    event Action? OnActiveBuildingChanged;

    /// <summary>
    /// Nastaví aktivní budovu. Vyvolá OnActiveBuildingChanged.
    /// Noop pokud je id shodné s aktuálním.
    /// </summary>
    void SetActiveBuilding(string id);
}
