namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models;

/// <summary>
/// Centrální kontejner stavu budovy (Observer pattern).
/// Singleton služba: simulace zapisuje, UI komponenty čtou a odebírají události.
/// Rozhraní umožňuje dependency inversion — komponenty závisí na abstrakci,
/// nikoliv na konkrétní implementaci.
/// </summary>
public interface IBuildingStateService
{
    /// <summary>Strukturální definice budovy (immutabilní po inicializaci).</summary>
    Building Building { get; }

    /// <summary>Aktuální stav konkrétního zařízení. Vrací null pro neznámé ID.</summary>
    DeviceState? GetDeviceState(string deviceId);

    /// <summary>Všechny stavy zařízení jako read-only snapshot.</summary>
    IReadOnlyDictionary<string, DeviceState> AllDeviceStates { get; }

    /// <summary>
    /// Aktualizace stavu jednoho zařízení. Volá simulační služba.
    /// Nevyvolává event — po dávce aktualizací zavolat NotifyStateChanged().
    /// </summary>
    void UpdateDeviceState(string deviceId, DeviceState newState);

    /// <summary>
    /// Vyvolání OnStateChanged eventu. Volat po dokončení dávky aktualizací.
    /// Oddělení aktualizace od notifikace umožňuje efektivní dávkování.
    /// </summary>
    void NotifyStateChanged();

    /// <summary>
    /// Event vyvolaný po změně stavu. Blazor komponenty se přihlašují k odběru.
    /// Handlery MUSÍ volat InvokeAsync(StateHasChanged) pro thread safety.
    /// </summary>
    event Action? OnStateChanged;
}
