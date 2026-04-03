namespace DiplomovaPrace.Models.Configuration;

/// <summary>
/// Konfigurační model budovy připravený pro EF Core persistenci.
/// Oddělen od doménového modelu Building, který slouží vizualizaci a simulaci.
/// RowVersion = EF Core [Timestamp] pro optimistickou konkurenci.
/// IsDeleted = soft delete (EF Core global query filter).
/// </summary>
public record BuildingConfig(
    string Id,
    string Name,
    string? Description,
    string? Address,
    IReadOnlyList<FloorConfig> Floors,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string CreatedBy,
    string UpdatedBy,
    byte[] RowVersion,
    bool IsDeleted,
    FacilityMetadata? Metadata = null
);
