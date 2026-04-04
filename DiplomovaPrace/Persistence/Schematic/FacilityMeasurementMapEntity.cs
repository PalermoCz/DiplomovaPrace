using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiplomovaPrace.Persistence.Schematic;

/// <summary>
/// EF Core persistence entita pro mapování uzlu schematu na měřicí bod.
/// Future-proof vrstva pro Sprint 3+ — umožní binding node → MeasurementRecord.DeviceId.
/// V Sprint 2 se pouze zakládá schéma, skutečné napojení přijde s KPI / analytics vrstvou.
/// </summary>
[Table("FacilityMeasurementMaps")]
public class FacilityMeasurementMapEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int FacilityId { get; set; }

    [ForeignKey(nameof(FacilityId))]
    public FacilityEntity Facility { get; set; } = null!;

    /// <summary>NodeKey uzlu, ke kterému se měření vztahuje.</summary>
    [Required]
    [MaxLength(100)]
    public string NodeKey { get; set; } = string.Empty;

    /// <summary>Technický identifikátor měřiče — odpovídá MeterUrn z mvp_nodes.csv.</summary>
    [Required]
    [MaxLength(200)]
    public string MeterUrn { get; set; } = string.Empty;

    /// <summary>
    /// Druh měření, např. "electricity", "heat", "cooling", "water".
    /// Slouží pro budoucí multi-energy analytics.
    /// </summary>
    [MaxLength(50)]
    public string MeasurementKind { get; set; } = "electricity";
}
