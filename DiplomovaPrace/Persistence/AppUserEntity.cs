using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiplomovaPrace.Persistence;

/// <summary>
/// EF Core persistence entita pro aplikačního uživatele.
/// První auth-shell milestone: lokální email + password auth.
/// V budoucnosti: rozšířit o FacilityMembership pro role/ownership management.
/// </summary>
[Table("AppUsers")]
public class AppUserEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Email — musí být unikátní a slouží jako login identifikátor.</summary>
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Hešovaní heslo (bcrypt) — nikdy neuchovávat plaintext.</summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Časové razítko vytvoření účtu.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Posledňí přihlášení — null pokud se ještě nikdy přihlásil.</summary>
    public DateTime? LastLoginUtc { get; set; }

    public ICollection<FacilityMembershipEntity> FacilityMemberships { get; set; } = new List<FacilityMembershipEntity>();
}
