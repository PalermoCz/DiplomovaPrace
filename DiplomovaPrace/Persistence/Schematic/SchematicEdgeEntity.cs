using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiplomovaPrace.Persistence.Schematic;

public static class SchematicRelationshipKinds
{
    public const string LayoutPrimary = "layout_primary";
    public const string AdditionalLink = "additional_link";
    public const string Membership = "membership";
    public const string Semantic = "semantic";
}

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

    /// <summary>
    /// Obecný typ vztahu v topologii. Layout parent-child se ukládá explicitně jako layout_primary,
    /// další vazby mohou být semantic, membership nebo jiný importovaný relationship kind.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string RelationshipKind { get; set; } = SchematicRelationshipKinds.Semantic;

    /// <summary>
    /// Kompatibilní příznak, že hrana reprezentuje primární layout vazbu.
    /// Layout stále řídí ParentNodeKey na uzlu, nikoliv edge traversal.
    /// </summary>
    public bool IsLayoutEdge { get; set; }

    /// <summary>Volitelná poznámka převzatá z importu nebo budoucího authoringu.</summary>
    [MaxLength(500)]
    public string? Note { get; set; }
}
