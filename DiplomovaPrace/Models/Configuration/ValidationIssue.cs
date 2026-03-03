namespace DiplomovaPrace.Models.Configuration;

/// <summary>Závažnost validačního problému.</summary>
public enum IssueType { Error, Warning }

/// <summary>
/// Jeden validační problém na flooru (překryv místností, zařízení mimo místnost, duplicitní název).
/// RoomId / DeviceId != null slouží ke zvýraznění problémového prvku v canvasu.
/// </summary>
public record ValidationIssue(
    string Message,
    IssueType Type,
    string? RoomId = null,
    string? DeviceId = null
);
