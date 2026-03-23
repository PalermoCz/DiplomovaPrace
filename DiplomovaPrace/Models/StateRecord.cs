namespace DiplomovaPrace.Models;

/// <summary>
/// Historický záznam stavu zařízení v časovém okamžiku.
/// Používáno pro time-series grafy (např. RadzenChart).
/// </summary>
public record StateRecord(
    DateTime Timestamp,
    double Value
);
