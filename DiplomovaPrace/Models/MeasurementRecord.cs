namespace DiplomovaPrace.Models;

/// <summary>
/// Detailní záznam elektrického měření v jednom časovém okamžiku.
/// Doplňuje jednoduchý StateRecord (Timestamp + Value) o kompletní elektrotechnické veličiny.
/// Navržen pro budoucí EF Core persistenci (vlastní tabulka s časovým indexem).
///
/// Konvence:
///   - Nullable pole: ne všechny měřicí body poskytují všechny veličiny.
///   - ActiveEnergyKWh je kumulativní (monotónně rostoucí) — odpovídá stavu elektroměru.
///   - Ostatní veličiny jsou okamžité (instantaneous).
/// </summary>
public record MeasurementRecord(
    /// <summary>Časový okamžik měření.</summary>
    DateTime Timestamp,

    /// <summary>ID měřicího bodu (Device.Id).</summary>
    string DeviceId,

    // ── Energie (kumulativní) ────────────────────────────────────────────

    /// <summary>Činná energie — kumulativní odběr od instalace (kWh).</summary>
    double? ActiveEnergyKWh,

    /// <summary>Jalová energie — kumulativní (kVArh).</summary>
    double? ReactiveEnergyKVArh,

    // ── Výkon (okamžitý) ─────────────────────────────────────────────────

    /// <summary>Činný výkon — aktuální odběr (kW).</summary>
    double? ActivePowerKW,

    /// <summary>Jalový výkon (kVAr).</summary>
    double? ReactivePowerKVAr,

    /// <summary>Zdánlivý výkon (kVA).</summary>
    double? ApparentPowerKVA,

    // ── Síťové veličiny (okamžité) ───────────────────────────────────────

    /// <summary>Sdružené / fázové napětí (V).</summary>
    double? VoltageV,

    /// <summary>Proud (A).</summary>
    double? CurrentA,

    /// <summary>Účiník — Power Factor (bezrozměrný, 0.0–1.0).</summary>
    double? PowerFactor,

    /// <summary>Frekvence sítě (Hz). Obvykle ~50 Hz v ČR.</summary>
    double? FrequencyHz
);
