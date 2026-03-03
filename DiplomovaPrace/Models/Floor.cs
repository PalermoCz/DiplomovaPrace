namespace DiplomovaPrace.Models;

/// <summary>
/// Patro budovy. Obsahuje seznam místností.
/// </summary>
public record Floor(
    string Id,
    string Name,
    int Level,
    string BuildingId,
    IReadOnlyList<Room> Rooms
);
