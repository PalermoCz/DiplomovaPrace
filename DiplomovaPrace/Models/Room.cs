namespace DiplomovaPrace.Models;

/// <summary>
/// Geometrie místnosti pro vykreslení jako SVG obdélník.
/// Pro složitější tvary lze rozšířit o SvgPathData string.
/// </summary>
public record RoomGeometry(
    double X,
    double Y,
    double Width,
    double Height
);

/// <summary>
/// Místnost v rámci patra. Obsahuje seznam zařízení a SVG geometrii.
/// </summary>
public record Room(
    string Id,
    string Name,
    string FloorId,
    RoomGeometry Geometry,
    IReadOnlyList<Device> Devices
);
