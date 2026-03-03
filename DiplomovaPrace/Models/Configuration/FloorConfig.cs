namespace DiplomovaPrace.Models.Configuration;

/// <summary>
/// Konfigurační model patra. ViewBoxWidth/Height umožňuje per-floor canvas velikost.
/// Výchozí hodnoty 800×300 odpovídají stávajícímu SVG viewBox.
/// </summary>
public record FloorConfig(
    string Id,
    string BuildingId,
    string Name,
    int Level,
    string? Description,
    double ViewBoxWidth,
    double ViewBoxHeight,
    IReadOnlyList<RoomConfig> Rooms,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsDeleted
);
