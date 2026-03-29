using DiplomovaPrace.Models;

namespace DiplomovaPrace.Services;

/// <summary>
/// Repository kontrakt pro perzistenci a čtení měřicích záznamů.
///
/// Implementace:
///   - EfMeasurementRepository → SQLite přes EF Core (Fáze 2A — aktuální)
///   - CsvMeasurementRepository → bulk import z CSV souboru (Fáze 3)
///
/// Lifecycle poznámka:
///   Implementace musí být bezpečná pro volání ze Singleton kontextu.
///   Doporučený přístup: IDbContextFactory&lt;AppDbContext&gt; (viz EfMeasurementRepository).
/// </summary>
public interface IMeasurementRepository
{
    // ── Zápis ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Uloží jeden záznam měření do perzistentního úložiště.
    /// Fire-and-forget varianta je dostupná přes SaveAsync (nezávisí na volajícím).
    /// </summary>
    Task SaveAsync(MeasurementRecord measurement, CancellationToken ct = default);

    /// <summary>
    /// Dávkové uložení — efektivnější než volání SaveAsync N-krát.
    /// Vhodné pro CSV import.
    /// </summary>
    Task SaveBatchAsync(IEnumerable<MeasurementRecord> measurements, CancellationToken ct = default);

    // ── Čtení ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Vrátí záznamy pro dané zařízení v časovém rozsahu [from, to].
    /// Výsledky jsou seřazeny vzestupně dle Timestamp.
    /// </summary>
    Task<IReadOnlyList<MeasurementRecord>> GetRangeAsync(
        string deviceId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    /// <summary>
    /// Vrátí posledních N záznamů pro dané zařízení, seřazených od nejnovějšího.
    /// </summary>
    Task<IReadOnlyList<MeasurementRecord>> GetLatestAsync(
        string deviceId,
        int count = 100,
        CancellationToken ct = default);

    /// <summary>
    /// Vrátí poslední záznam pro dané zařízení. Null pokud žádný neexistuje.
    /// Vhodné pro zobrazení "posledního stavu" v dashboardu.
    /// </summary>
    Task<MeasurementRecord?> GetLatestOneAsync(
        string deviceId,
        CancellationToken ct = default);

    /// <summary>
    /// Vrátí všechna ID zařízení, pro která existují záznamy v DB.
    /// Vhodné pro discovery / seznam měřicích bodů.
    /// </summary>
    Task<IReadOnlyList<string>> GetAvailableDeviceIdsAsync(CancellationToken ct = default);

    // ── Správa dat ────────────────────────────────────────────────────────

    /// <summary>
    /// Smaže záznamy starší než zadaný datum.
    /// Vhodné pro periodický cleanup — aby DB nerostla donekonečna.
    /// </summary>
    Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);

    /// <summary>
    /// Vrátí počet záznamů pro dané zařízení.
    /// Vhodné pro diagnostiku a monitoring.
    /// </summary>
    Task<long> CountAsync(string deviceId, CancellationToken ct = default);
}
