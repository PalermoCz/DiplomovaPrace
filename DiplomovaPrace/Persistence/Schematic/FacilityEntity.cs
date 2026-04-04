using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiplomovaPrace.Persistence.Schematic;

/// <summary>
/// EF Core persistence entita pro facility (provozovnu/závod).
/// Zastřešuje schematic graf (nodes + edges) a je top-level agregát pro facility-centric model.
/// </summary>
[Table("Facilities")]
public class FacilityEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Název facility — musí být unikátní (unique index).</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>IANA timezone identifikátor, např. "Europe/Prague".</summary>
    [MaxLength(100)]
    public string TimeZone { get; set; } = "UTC";

    // ── Navigation ────────────────────────────────────────────────────────────

    public ICollection<SchematicNodeEntity> Nodes { get; set; } = new List<SchematicNodeEntity>();
    public ICollection<SchematicEdgeEntity> Edges { get; set; } = new List<SchematicEdgeEntity>();
    public ICollection<FacilityMeasurementMapEntity> MeasurementMaps { get; set; } = new List<FacilityMeasurementMapEntity>();
}
