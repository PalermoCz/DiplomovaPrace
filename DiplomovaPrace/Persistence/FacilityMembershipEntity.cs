using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DiplomovaPrace.Persistence.Schematic;

namespace DiplomovaPrace.Persistence;

[Table("FacilityMemberships")]
public class FacilityMembershipEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int FacilityId { get; set; }

    [Required]
    public int AppUserId { get; set; }

    [Required]
    [MaxLength(32)]
    public string Role { get; set; } = FacilityMembershipRole.Viewer.ToString();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public FacilityEntity? Facility { get; set; }
    public AppUserEntity? AppUser { get; set; }
}
