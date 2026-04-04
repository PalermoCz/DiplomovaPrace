using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiplomovaPrace.Persistence.Schematic;

/// <summary>
/// EF Core persistence entita pro jednu hranu schematického grafu facility.
/// Odpovídá jednomu řádku z mvp_edges.csv nebo automaticky dopočítané hraně z parent_node_id.
/// </summary>
[Table("SchematicEdges")]
public class SchematicEdgeEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int FacilityId { get; set; }

    [ForeignKey(nameof(FacilityId))]
    public FacilityEntity Facility { get; set; } = null!;

    /// <summary>NodeKey zdrojového uzlu hrany.</summary>
    [Required]
    [MaxLength(100)]
    public string SourceNodeKey { get; set; } = string.Empty;

    /// <summary>NodeKey cílového uzlu hrany.</summary>
    [Required]
    [MaxLength(100)]
    public string TargetNodeKey { get; set; } = string.Empty;
}
