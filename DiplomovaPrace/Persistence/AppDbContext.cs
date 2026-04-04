using DiplomovaPrace.Persistence.Schematic;
using Microsoft.EntityFrameworkCore;

namespace DiplomovaPrace.Persistence;

/// <summary>
/// Hlavní EF Core DbContext aplikace.
///
/// Fáze 2A: obsahuje tabulku MeasurementRecords.
/// Fáze 2B (Sprint 2): přidány facility-centric tabulky pro schematic-first model.
///
/// Lifecycle: registrován jako IDbContextFactory&lt;AppDbContext&gt; (Singleton).
/// Každé repository si vytváří vlastní scope pomocí factory.CreateDbContext()
/// a ihned ho disposuje po operaci — bezpečné pro použití ze Singleton služeb.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Původní měřicí data ───────────────────────────────────────────────────

    /// <summary>Tabulka s detailními záznamy elektrických měření.</summary>
    public DbSet<MeasurementRecordEntity> MeasurementRecords => Set<MeasurementRecordEntity>();

    // ── Facility-centric schematic model (Sprint 2) ───────────────────────────

    /// <summary>Facility — top-level agregát schematic grafu.</summary>
    public DbSet<FacilityEntity> Facilities => Set<FacilityEntity>();

    /// <summary>Uzly schematického grafu facility.</summary>
    public DbSet<SchematicNodeEntity> SchematicNodes => Set<SchematicNodeEntity>();

    /// <summary>Hrany schematického grafu facility (backbone + dopočítané z parent_node_id).</summary>
    public DbSet<SchematicEdgeEntity> SchematicEdges => Set<SchematicEdgeEntity>();

    /// <summary>Mapování uzlů na měřicí body — future-proof vrstva pro Sprint 3+.</summary>
    public DbSet<FacilityMeasurementMapEntity> FacilityMeasurementMaps => Set<FacilityMeasurementMapEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── MeasurementRecords (původní konfigurace) ──────────────────────────

        modelBuilder.Entity<MeasurementRecordEntity>(entity =>
        {
            entity.ToTable("MeasurementRecords");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.DeviceId, e.Timestamp })
                  .HasDatabaseName("IX_MeasurementRecords_DeviceId_Timestamp");

            entity.HasIndex(e => e.Timestamp)
                  .HasDatabaseName("IX_MeasurementRecords_Timestamp");

            entity.Property(e => e.DeviceId)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Timestamp)
                  .HasConversion(
                      v => v.ToUniversalTime().ToString("O"),
                      v => DateTime.Parse(v, null, System.Globalization.DateTimeStyles.RoundtripKind));
        });

        // ── FacilityEntity ────────────────────────────────────────────────────

        modelBuilder.Entity<FacilityEntity>(entity =>
        {
            entity.ToTable("Facilities");
            entity.HasKey(e => e.Id);

            // Název facility musí být unikátní
            entity.HasIndex(e => e.Name)
                  .IsUnique()
                  .HasDatabaseName("IX_Facilities_Name");
        });

        // ── SchematicNodeEntity ───────────────────────────────────────────────

        modelBuilder.Entity<SchematicNodeEntity>(entity =>
        {
            entity.ToTable("SchematicNodes");
            entity.HasKey(e => e.Id);

            // Unikátní klíč uzlu v rámci facility
            entity.HasIndex(e => new { e.FacilityId, e.NodeKey })
                  .IsUnique()
                  .HasDatabaseName("IX_SchematicNodes_FacilityId_NodeKey");

            entity.HasOne(e => e.Facility)
                  .WithMany(f => f.Nodes)
                  .HasForeignKey(e => e.FacilityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── SchematicEdgeEntity ───────────────────────────────────────────────

        modelBuilder.Entity<SchematicEdgeEntity>(entity =>
        {
            entity.ToTable("SchematicEdges");
            entity.HasKey(e => e.Id);

            // Hrana musí být unikátní v rámci facility (zamezení duplicit)
            entity.HasIndex(e => new { e.FacilityId, e.SourceNodeKey, e.TargetNodeKey })
                  .IsUnique()
                  .HasDatabaseName("IX_SchematicEdges_FacilityId_Source_Target");

            entity.HasOne(e => e.Facility)
                  .WithMany(f => f.Edges)
                  .HasForeignKey(e => e.FacilityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── FacilityMeasurementMapEntity ──────────────────────────────────────

        modelBuilder.Entity<FacilityMeasurementMapEntity>(entity =>
        {
            entity.ToTable("FacilityMeasurementMaps");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Facility)
                  .WithMany(f => f.MeasurementMaps)
                  .HasForeignKey(e => e.FacilityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
