using Microsoft.EntityFrameworkCore;

namespace DiplomovaPrace.Persistence;

/// <summary>
/// Hlavní EF Core DbContext aplikace.
///
/// Fáze 2A: obsahuje pouze tabulku MeasurementRecords.
/// Konfigurace budovy (Building/Floor/Room/Device) zůstává in-memory —
/// bude přidána v pozdější fázi.
///
/// Lifecycle: registrován jako IDbContextFactory&lt;AppDbContext&gt; (Singleton).
/// Každé repository si vytváří vlastní scope pomocí factory.CreateDbContext()
/// a ihned ho disposuje po operaci — bezpečné pro použití ze Singleton služeb.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>Tabulka s detailními záznamy elektrických měření.</summary>
    public DbSet<MeasurementRecordEntity> MeasurementRecords => Set<MeasurementRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MeasurementRecordEntity>(entity =>
        {
            entity.ToTable("MeasurementRecords");
            entity.HasKey(e => e.Id);

            // ── Indexy pro výkonné dotazy ─────────────────────────────────────────

            // Primární dotazovací pattern: "dej mi data pro zařízení X za posledních Y hodin"
            entity.HasIndex(e => new { e.DeviceId, e.Timestamp })
                  .HasDatabaseName("IX_MeasurementRecords_DeviceId_Timestamp");

            // Sekundární: cleanup a stránkování dle času
            entity.HasIndex(e => e.Timestamp)
                  .HasDatabaseName("IX_MeasurementRecords_Timestamp");

            // ── Omezení sloupců ───────────────────────────────────────────────────
            entity.Property(e => e.DeviceId)
                  .IsRequired()
                  .HasMaxLength(100);

            // DateTime → TEXT v SQLite (bezpečnější než numerický ticks)
            entity.Property(e => e.Timestamp)
                  .HasConversion(
                      v => v.ToUniversalTime().ToString("O"),
                      v => DateTime.Parse(v, null, System.Globalization.DateTimeStyles.RoundtripKind));
        });
    }
}
