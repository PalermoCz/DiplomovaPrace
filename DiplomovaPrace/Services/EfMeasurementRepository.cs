using DiplomovaPrace.Models;
using DiplomovaPrace.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiplomovaPrace.Services;

/// <summary>
/// EF Core implementace IMeasurementRepository pro SQLite.
///
/// ── Lifecycle řešení ──────────────────────────────────────────────────────
/// Problém: SimulationService a BuildingStateService jsou Singleton.
///          DbContext je Scoped → nelze injektovat přímo do Singleton.
///
/// Řešení: IDbContextFactory&lt;AppDbContext&gt; (Singleton factory).
///         Každá metoda si vytvoří vlastní krátkožijící DbContext,
///         provede operaci a okamžitě ho disposuje (using var db = ...).
///         Tím nedochází ke "scoped-in-singleton" chybě.
///
/// ── Thread safety ─────────────────────────────────────────────────────────
/// DbContext není thread-safe. Proto každá metoda vytváří nový context.
/// Pro batch operace je použit jeden context po dobu celé dávky.
/// </summary>
public class EfMeasurementRepository : IMeasurementRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<EfMeasurementRepository> _logger;

    public EfMeasurementRepository(
        IDbContextFactory<AppDbContext> factory,
        ILogger<EfMeasurementRepository> logger)
    {
        _factory = factory;
        _logger  = logger;
    }

    // ── Zápis ─────────────────────────────────────────────────────────────

    public async Task SaveAsync(MeasurementRecord measurement, CancellationToken ct = default)
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            var entity = MeasurementRecordMapper.ToEntity(measurement);
            db.MeasurementRecords.Add(entity);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při ukládání měření pro zařízení {DeviceId}", measurement.DeviceId);
            // Throw záměrně NEpropagujeme — simulační smyčka nesmí padat kvůli DB chybě
        }
    }

    public async Task SaveBatchAsync(IEnumerable<MeasurementRecord> measurements, CancellationToken ct = default)
    {
        var batch = measurements.ToList();
        if (batch.Count == 0) return;

        try
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            var entities = batch.Select(MeasurementRecordMapper.ToEntity).ToList();
            db.MeasurementRecords.AddRange(entities);
            await db.SaveChangesAsync(ct);
            _logger.LogDebug("Uloženo {Count} měření do DB", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při dávkovém ukládání {Count} měření", batch.Count);
            throw; // Dávkový import má chybu propagovat (CSV import potřebuje vědět)
        }
    }

    // ── Čtení ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<MeasurementRecord>> GetRangeAsync(
        string deviceId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        // EF Core překládá DateTime.CompareTo přes nakonfigurovaný value converter
        // (ISO-8601 string v SQLite) — řazení funguje správně díky formátu "O"
        var entities = await db.MeasurementRecords
            .Where(e => e.DeviceId == deviceId
                     && e.Timestamp >= from
                     && e.Timestamp <= to)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(ct);

        return entities.Select(MeasurementRecordMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<MeasurementRecord>> GetLatestAsync(
        string deviceId, int count = 100, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var entities = await db.MeasurementRecords
            .Where(e => e.DeviceId == deviceId)
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToListAsync(ct);

        // Vrátit vzestupně (nejstarší→nejnovější) pro grafy
        entities.Reverse();
        return entities.Select(MeasurementRecordMapper.ToDomain).ToList();
    }

    public async Task<MeasurementRecord?> GetLatestOneAsync(
        string deviceId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var entity = await db.MeasurementRecords
            .Where(e => e.DeviceId == deviceId)
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefaultAsync(ct);

        return entity is null ? null : MeasurementRecordMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<string>> GetAvailableDeviceIdsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.MeasurementRecords
            .Select(e => e.DeviceId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(ct);
    }

    // ── Správa dat ────────────────────────────────────────────────────────

    public async Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        // ExecuteDeleteAsync = EF 7+ bulk delete bez načítání do paměti
        var deleted = await db.MeasurementRecords
            .Where(e => e.Timestamp < cutoff)
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation("Smazáno {Count} starých měření (před {Cutoff:O})", deleted, cutoff);
    }

    public async Task<long> CountAsync(string deviceId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.MeasurementRecords
            .LongCountAsync(e => e.DeviceId == deviceId, ct);
    }
}
