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

    /// <summary>
    /// Získá historii stavů zařízení pro vykreslení grafu.
    /// Vrací až posledních 100 záznamů (ring buffer).
    /// </summary>
    IReadOnlyList<StateRecord> GetDeviceHistory(string deviceId);

    /// <summary>
    /// Vymaže historii všech zařízení (např. při resetu simulace).
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Nahradí strukturu budovy novou konfigurací z editoru.
    /// Vymaže existující stavy zařízení a inicializuje výchozí stavy pro nová zařízení.
    /// Vyvolá OnStateChanged po dokončení výměny.
    /// </summary>
    void ReplaceBuilding(Building newBuilding);

    // ── Smart metering: detailní měření ──────────────────────────────────────

    /// <summary>
    /// Přidá detailní MeasurementRecord pro metering zařízení.
    /// Ring buffer: uchovává max 100 posledních záznamů per device.
    /// </summary>
    void AddMeasurement(string deviceId, MeasurementRecord measurement);

    /// <summary>
    /// Vrací posledních N detailních měření pro daný měřicí bod.
    /// </summary>
    IReadOnlyList<MeasurementRecord> GetMeasurements(string deviceId);

    /// <summary>
    /// Vrací poslední detailní měření pro daný měřicí bod. Null pokud neexistuje.
    /// </summary>
    MeasurementRecord? GetLatestMeasurement(string deviceId);
}
