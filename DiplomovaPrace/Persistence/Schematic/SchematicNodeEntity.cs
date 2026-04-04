using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiplomovaPrace.Persistence.Schematic;

/// <summary>
/// EF Core persistence entita pro jeden uzel schematického grafu facility.
/// Odpovídá jednomu řádku z mvp_nodes.csv.
/// </summary>
[Table("SchematicNodes")]
public class SchematicNodeEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int FacilityId { get; set; }

    [ForeignKey(nameof(FacilityId))]
    public FacilityEntity Facility { get; set; } = null!;

    /// <summary>Technický klíč uzlu z CSV (node_id), unikátní v rámci facility.</summary>
    [Required]
    [MaxLength(100)]
    public string NodeKey { get; set; } = string.Empty;

    /// <summary>Zobrazitelný popis uzlu.</summary>
    [Required]
    [MaxLength(200)]
    public string Label { get; set; } = string.Empty;

    /// <summary>Typ uzlu (transformer, zone, subzone, server, …).</summary>
    [MaxLength(100)]
    public string? NodeType { get; set; }

    /// <summary>Funkční zóna, do které uzel patří.</summary>
    [MaxLength(100)]
    public string? Zone { get; set; }

    /// <summary>Technický identifikátor měřiče, pokud uzel má přímé měření.</summary>
    [MaxLength(200)]
    public string? MeterUrn { get; set; }

    /// <summary>Rodičovský uzel v hierarchii (node_id rodiče, nebo null pro kořenové uzly).</summary>
    [MaxLength(100)]
    public string? ParentNodeKey { get; set; }

    /// <summary>Indikativní X souřadnice pro layout (0.0–1.0 relativně).</summary>
    public double? XHint { get; set; }

    /// <summary>Indikativní Y souřadnice pro layout (0.0–1.0 relativně).</summary>
    public double? YHint { get; set; }
}
