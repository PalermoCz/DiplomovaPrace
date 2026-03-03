using DiplomovaPrace.Models;

namespace DiplomovaPrace.Models.Configuration;

/// <summary>
/// Konfigurační model místnosti. RoomGeometry je sdílený value object s vizualizační doménou.
/// FillColorOverride = null znamená použití StateColorMapper výchozí barvy.
/// </summary>
public record RoomConfig(
    string Id,
    string FloorId,
    string Name,
    RoomGeometry Geometry,
    string? FillColorOverride,
    IReadOnlyList<DeviceConfig> Devices,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsDeleted
);
