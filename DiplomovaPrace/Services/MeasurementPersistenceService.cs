using DiplomovaPrace.Models;
using System.Threading.Channels;

namespace DiplomovaPrace.Services;

/// <summary>
/// Background service, která asynchronně přebírá MeasurementRecord záznamy
/// ze simulace/importu a ukládá je do DB přes IMeasurementRepository.
///
/// ── Architektura (Producer–Consumer přes Channel) ─────────────────────────
///
///   SimulationService (Singleton)
///       │  .EnqueueAsync(record)  ← neblokující, &lt; 1 μs
///       ▼
///   MeasurementPersistenceService (IHostedService, Singleton)
///       │  Channel&lt;MeasurementRecord&gt; (bounded, kapacita 5000)
///       ▼
///   EfMeasurementRepository.SaveBatchAsync()  ← flush každé 2s nebo 100 ms batch
///       ▼
///   SQLite DB
///
/// ── Výhody ────────────────────────────────────────────────────────────────
///   - SimulationService neblokuje na DB (fire-and-forget)
///   - Dávkové ukládání snižuje počet DB transakcí
///   - Channel backpressure: pokud channel přetéká (backlog > 5000),
///     EnqueueAsync zahodí záznam (TryWrite) — DB nesmí blokovat UI
///
/// ── Flushování ────────────────────────────────────────────────────────────
///   Batche jsou flushovány po zaplnění (100 záznamů) nebo po 2 sekundách —
///   podle toho, co nastane dříve.
/// </summary>
public class MeasurementPersistenceService : BackgroundService
{
    private readonly IMeasurementRepository _repository;
    private readonly ILogger<MeasurementPersistenceService> _logger;

    // Bounded channel: pokud je v zásobníku více než 5000 záznamů,
    // nové záznamy jsou zahozeny (Writer.TryWrite vrátí false).
    private readonly Channel<MeasurementRecord> _channel =
        Channel.CreateBounded<MeasurementRecord>(new BoundedChannelOptions(5000)
        {
            FullMode   = BoundedChannelFullMode.DropOldest,
            SingleReader = true   // jeden consumer (tato služba)
        });

    private const int BatchSize    = 100;      // počet záznamů před flush
    private const int FlushMs      = 2_000;    // max čekání na flush (ms)

    public MeasurementPersistenceService(
        IMeasurementRepository repository,
        ILogger<MeasurementPersistenceService> logger)
    {
        _repository = repository;
        _logger     = logger;
    }

    /// <summary>
    /// Zařadí záznam do fronty pro asynchronní uložení. Neblokující.
    /// Pokud je fronta plná, záznam je zahozen (starší záznamy, viz DropOldest).
    /// </summary>
    public void Enqueue(MeasurementRecord record)
    {
        if (!_channel.Writer.TryWrite(record))
        {
            _logger.LogWarning("MeasurementPersistence: kanál plný, záznam zahozen pro {DeviceId}", record.DeviceId);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MeasurementPersistenceService: zahájena konzumace záznamu");

        var batch = new List<MeasurementRecord>(BatchSize);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Čtení prvního záznamu — čeká max FlushMs, pak flush prázdné/neprázdné dávky
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(FlushMs);

                try
                {
                    await foreach (var record in _channel.Reader.ReadAllAsync(cts.Token))
                    {
                        batch.Add(record);
                        if (batch.Count >= BatchSize) break;
                    }
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // FlushMs timeout vypršel — flush co máme
                }

                if (batch.Count > 0)
                {
                    await FlushBatchAsync(batch, stoppingToken);
                    batch.Clear();
                }
            }
        }
        finally
        {
            // Při shutdown flushnout co zbývá v kanálu
            _channel.Writer.Complete();

            await foreach (var record in _channel.Reader.ReadAllAsync())
                batch.Add(record);

            if (batch.Count > 0)
                await FlushBatchAsync(batch, CancellationToken.None);

            _logger.LogInformation("MeasurementPersistenceService: ukončena");
        }
    }

    private async Task FlushBatchAsync(List<MeasurementRecord> batch, CancellationToken ct)
    {
        try
        {
            await _repository.SaveBatchAsync(batch, ct);
            _logger.LogDebug("MeasurementPersistence: flushnuto {Count} záznamů", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MeasurementPersistence: chyba při flush {Count} záznamů", batch.Count);
        }
    }
}
